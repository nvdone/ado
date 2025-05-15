// NVD Alter Database Objects
// Copyright © 2015, Nikolay Dudkin
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with this program.If not, see<https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using NVD.SQL;

namespace ado
{
	internal class ado
	{
		static readonly Regex re_crlf = new Regex("[\\r\\n]");
		static readonly Regex re_tabs = new Regex("\\t{2,}");
		static readonly Regex re_spcs = new Regex("[^\\S\\t]{2,}");

		static string merge(string str)
		{
			return re_spcs.Replace(re_tabs.Replace(re_crlf.Replace(str, "\t"), "\t"), " ");
		}

		static void Main(string[] args)
		{
			try
			{
				Console.WriteLine("NVD Alter Database Objects\r\n(C) 2015, Nikolay Dudkin\r\n");
				if (args.Length < 3)
				{
					Console.WriteLine("Usage: ado.exe server database_list.txt rule_list.txt [-user:<username>] [-pass:<password>] [-commit]");
					return;
				}

				string server = args[0];
				string db_list_txt = args[1];
				string rule_list_txt = args[2];
				string user = "";
				string pass = "";
				bool commit = false;

				for(int i = 3; i < args.Length; i++)
				{
					if(args[i].StartsWith("-user:", StringComparison.InvariantCultureIgnoreCase))
						user = args[i].Substring(6);

					if (args[i].StartsWith("-pass:", StringComparison.InvariantCultureIgnoreCase))
						pass = args[i].Substring(6);

					if (args[i].Equals("-commit", StringComparison.InvariantCultureIgnoreCase))
						commit = true;
				}

				List<string> db_names = File.ReadAllLines(db_list_txt).Where(str => str.Length > 0 && !str.StartsWith("//")).ToList();

				List<(Regex re_view, Regex re_content, string subst)> rules = new List<(Regex, Regex, string)>();
				foreach (string rule in File.ReadAllLines(rule_list_txt).Where(str => str.Length > 0 && !str.StartsWith("//")))
				{
					string[] parts = rule.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length < 2 || parts.Length > 4)
					{
						Console.WriteLine($"INVALID RULE: {rule}\r\n");
						continue;
					}

					rules.Add((new Regex(parts[0]), new Regex(parts[1]), parts.Length == 3 ? parts[2] : ""));
				}

				Regex re_ca = new Regex("CREATE\\s+(\\w+)\\s+(\\S+)");

				foreach (string db_name in db_names)
				{
					MSSQL db = null;
					if (user.Length > 0 && pass.Length > 0)
						db = new MSSQL($"Server={server};Database={db_name};User Id={user};Password={pass};TrustServerCertificate=True;");
					else
						db = new MSSQL($"Server={server};Database={db_name};Integrated Security=true;TrustServerCertificate=True;");

					List<Tuple<string, string, string>> items = null;
					try
					{
						items = db.GetTupleList<string, string, string>($"SELECT name, schema_name(schema_id), OBJECT_DEFINITION(object_id) FROM sys.objects WHERE type IN ('FN', 'IF', 'P', 'V', 'TR', 'TF', 'X');");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"FAILED TO GET OBJECTS FOR {db_name}: {merge(ex.Message)}\r\n");
						continue;
					}

					foreach (var item in items)
					{
						string object_schema_name = $"{item.Item2}.{item.Item1}";
						string object_def = item.Item3;

						foreach (var (re_view, re_content, susbt) in rules)
						{
							if (re_view.IsMatch(object_schema_name) && re_content.IsMatch(object_def))
							{
								object_def = re_content.Replace(object_def, susbt);
							}
						}

						if (!object_def.Equals(item.Item3))
						{
							if (re_ca.IsMatch(object_def))
							{
								object_def = re_ca.Replace(object_def, $"ALTER $1 {object_schema_name}");

								if (commit)
								{
									try
									{
										db.Execute(object_def);
									}
									catch (Exception ex)
									{
										Console.WriteLine($"FAILED TO ALTER {db_name}.{object_schema_name}: {merge(ex.Message)}\r\n");
										continue;
									}
									Console.WriteLine($"ALTERED {db_name}.{object_schema_name}:\r\n{merge(item.Item3)}\r\n{merge(object_def)}\r\n");
								}
								else
									Console.WriteLine($"CAN ALTER {db_name}.{object_schema_name}:\r\n{merge(item.Item3)}\r\n{merge(object_def)}\r\n");
							}
							else
							{
								if(commit)
									Console.WriteLine($"FAILED TO ALTER {db_name}.{object_schema_name}: CREATE statement not recognized.\r\n");
								else
									Console.WriteLine($"WILL FAIL TO ALTER {db_name}.{object_schema_name}: CREATE statement not recognized.\r\n");
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"EXCEPTION: {merge(ex.Message)}");
			}
		}
	}
}
