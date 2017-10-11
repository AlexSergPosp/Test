#if  UNITY_EDITOR
 

using UnityEditor;
using UnityEngine;
using System.Linq;

using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using UniRx;


public class Row
{
    public OrderedDictionary data;
    public int Length { get { return data.Count; } }
    public int index = -1;
    public List<Row> table;
    public IList<string> Keys { get { return data.Keys.Cast<string>().ToList(); } }

    public string this[string i]
    {
        get { return data[i] as string; }
    }

    public string this[int i]
    {
        get { return data[i] as string; }
    }

    public Row(OrderedDictionary fill)
    {
        data = fill;
    }

    public bool has(string key)
    {
        return data.Contains(key) && data[key] as string != "";
    }

    public T CastOrDefault<T>(string key, T def = default(T)) where T : new()
    {
        if (has(key))
            return this[key].CastTo<T>();
        return def;
    }

    public string AtOrDefault(string key, string def = "")
    {
        return has(key) ? this[key] : def;
    }


    public T[] GetArray<T>(int offset, int count) where T : new()
    {
        T[] result = new T[count];

        for (int i = offset, r = 0; i < offset + count; i++, r++)
        {
            result[r] = (data[i] as string).CastTo<T>();
        }

        return result;
    }

    public T EnumOrDefault<T>(string key, T def = default(T))
    {
        if (has(key))
            return (T)Enum.Parse(typeof(T), this[key]);
        return def;
    }

    public int Count { get { return data.Count; } }

    public string GetLast(string Key)
    {
        if (has(Key))
            return this[Key];
        else
        {
            var currentRow = this;
            while (currentRow.prev != null)
            {
                currentRow = currentRow.prev;

                if (currentRow.has(Key))
                    return currentRow[Key];
            }

            throw new FormatException(Key + " not found; at[" + index + "]");
        }
    }

    public CsvCell GetCell(string key)
    {
        return new CsvCell(index, data.GetIndex(key), table);
    }

    public Row next = null;
    public Row prev = null;
}

public class CsvCell
{
    private int x = 0, y = 0;
    List<Row> table;

    public CsvCell(int _x, int _y, List<Row> _table)
    {
        x = _x;
        y = _y;
        table = _table;
    }

    public bool IsAttribute()
    {
        return table[x].Keys[y].StartsWith("Attr", StringComparison.InvariantCultureIgnoreCase);
    }

    public CsvCell GetOffset(int _x, int _y)
    {
        return new CsvCell(x + _x, y + _y, table);
    }

    public bool hasValue()
    {
        return table != null && x < table.Count && y < table[x].Count && table[x][y] != "";
    }

    public string GetValue()
    {
        try
        {
            if (x < table.Count && y < table[x].Count)
            {
                return table[x][y];
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        return "";
    }
}


public class CsvReader : IEnumerable<Row>
{
    List<string> columns;
    public List<Row> rows = new List<Row>();

    public IEnumerator<Row> GetEnumerator()
    {
        return rows.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public int rowCount { get { return rows.Count; } }
    public int colCount { get { return columns.Count; } }

    public Row atRow(int rowIndex)
    {
        return rows[rowIndex];
    }

    public Row atRow(string key, string value)
    {
        return rows.FirstOrDefault(val =>
        {
            return val.has(key) && val[key] == value;
        });
    }

    public Row AtRealRow(int rowIndex)
    {
        return atRow(rowIndex - 2);
    }

    public List<string> GetColumns()
    {
        return columns;
    }


    public List<string> Column(string name)
    {
        return rows.Select(row => row[name]).ToList();
    }


    public CsvReader(string fileName)
    {
        using (StreamReader r = new StreamReader(fileName))
        {
            string line;
            line = r.ReadLine();

            rows.Clear();

            if (line != null)
            {
                columns = line.Split(',').Select(v => v.Trim()).ToList();

                Debug.Log(fileName + " Header: " + line);
                int rowIndex = 0;
                while ((line = r.ReadLine()) != null)
                {
                    var rawCells = new List<string>();

                    try
                    {
                        bool good = true;
                        int lastBad = 0;
                        string accumulatedCell = "";
                        for (int i = 0; i < line.Length; i++)
                        {
                            if (line[i] == ',' && good)
                            {
                                rawCells.Add(accumulatedCell.Trim());
                                accumulatedCell = "";
                            }
                            else if (line.Length > (i + 1) && line[i] == '"' && line[i + 1] == '"')
                            {
                                accumulatedCell += '"';
                                i++;
                            }
                            else if (line[i] == '"')
                            {
                                good = !good;
                            }
                            else
                                accumulatedCell += line[i];
                        }

                        rawCells.Add(accumulatedCell);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message + e.StackTrace + ", at row " + rowIndex);
                    }

                    OrderedDictionary cells = new OrderedDictionary();

                    try
                    {
                        int indexCounter = 0;
                        foreach (string cell in rawCells)
                        {
                            if (indexCounter < columns.Count)
                                cells[columns[indexCounter]] = cell;
                            indexCounter++;
                        }

                        rows.Add(new Row(cells));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message + e.StackTrace + ", at row " + rowIndex);
                    }

                    rowIndex++;
                }

                for (int i = 0; i < rows.Count; i++)
                {
                    if (i > 0)
                    {
                        rows[i].prev = rows[i - 1];
                        rows[i - 1].next = rows[i];
                    }

                    rows[i].index = i;
                    rows[i].table = rows;
                }
            }
        }
    }
}

public class MyWindow : EditorWindow
{
    static string EventsPath = "Assets/Raw/events.csv";
    static string SinsPath = "Assets/Raw/sins.csv";
    static string TranslatePath = "Assets/Raw/translate.csv";

    static string baseDataUrl = "https://docs.google.com/spreadsheets/d/1cprpjujuC07IlwdbwSmz4ONhGd9UYpvoDvncz2-F898/pub?gid=";
    static string urlEnding = "&single=true&output=csv";

    static string urlEvents = baseDataUrl + "0" + urlEnding;
    static string urlSins = baseDataUrl + "648255533" + urlEnding;
    static string urlTranslate = baseDataUrl + "399128438" + urlEnding;




    static string finalPath = "Assets/Resources/StaticData.bytes";
    static GameData d;


   [MenuItem("ImportExcel/Parse")]
    public static void ShowWindow()
    {
        var finalFile = File.Open(finalPath, FileMode.Create);
        try
        {
            CsvReader eventsReader = new CsvReader(EventsPath);
            CsvReader upgradesReader = new CsvReader(SinsPath);

            d = new GameData();
            

            d.events = readEvents(eventsReader);
            d.upgrades = readUpgrades(upgradesReader);

            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(finalFile, d);

        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + e.StackTrace);
        }
    }

    public static  List<DeathEvent> readEvents(CsvReader rows)
    {
        var events = new List<DeathEvent>();

        foreach (var row in rows)
        {
            if (row.has("id"))
            {
                var ev = new DeathEvent();
                ev.id = row["id"];
                ev.name = row["name"];
                ev.desc = row["description"];
                if (int.Parse(row["isCool"]) == 1) ev.strench = float.Parse(row["value"]);
                else ev.strench = -float.Parse(row["value"]);
                events.Add(ev);
            }
            
        }
        return events;
    }
    public static  List<Upgrade> readUpgrades(CsvReader rows)
    {
        var upgrades = new List<Upgrade>();

        foreach (var row in rows)
        {
            if (row.has("id"))
            {
                var u = new Upgrade();
                u.id = row["id"];
                u.name = row["name"];
                u.desc = row["desc"];
                u.cost = new Resources();
                u.cost.count.value = long.Parse(row["cost"]);
                u.addResources = new Resources();
                u.addResources.count.value = long.Parse(row["value"]);
                u.upgradeType = (UpgradeType)Enum.Parse(typeof(UpgradeType), row["upgradeType"]);
                upgrades.Add(u);
            }
            
        }
        return upgrades;
    }

    [MenuItem("ImportExcel/DownloadFromNet")]
    public static void ShowWindow2()
    {
        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

        List<Thread> threads = new List<Thread>
        {
            Download(urlEvents, EventsPath),
            Download(urlSins, SinsPath),
            Download(urlTranslate, TranslatePath),
            
        };
        foreach (var thread in threads)
        {
            thread.Join();
        }
        Debug.Log("Downloaded");
    }

    private static Thread Download(string address, string path)
    {
        var thread = new Thread(() =>
        {
            var client = new WebClient();
            client.DownloadFile(address, path);
        });
        thread.Start();
        return thread;
    }
}
#endif


