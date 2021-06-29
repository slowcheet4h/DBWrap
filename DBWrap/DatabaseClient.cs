using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.X509.Qualified;

namespace DBWrap
{
    public class DatabaseClient
    {
        private MySqlConnection connection;

        public DatabaseClient(string user, string password, string database, string host = "localhost", int port = 3306)
        {
            connection =
                new MySqlConnection($"server={host};user={user};database={database};password={password};port={port}");
            connection.Open();
        } 
        
        public DatabaseClient(string connectionString)
        {
            connection =
                new MySqlConnection(connectionString);
            connection.Open();
        }

        private object actionMutex = new object();

        public int Execute(string sql, params object[] arguments)
        {
            MySqlCommand command = arguments.Length == 0
                ? new MySqlCommand(sql, connection)
                : CreateCommand(sql, arguments);
            
            return command.ExecuteNonQuery();
        }
        
        // todo: return empty array instead of returning null
        public object[] FirstRaw(string query, params object[] arguments)
        {
            MySqlCommand command = arguments.Length == 0
                ? new MySqlCommand(query, connection)
                : CreateCommand(query, arguments);
            MySqlDataReader dataReader;
            object[] objects;
            lock (actionMutex)
            {
                dataReader = command.ExecuteReader();
                if (dataReader.Read())
                {
                    objects = new object[dataReader.FieldCount];
                    dataReader.GetValues(objects);
                    dataReader.Close();
                }
                else
                {
                    dataReader.Close();
                    return null;
                }
            }
            
            
            return objects;
        }

        public Dictionary<string, object> First(string query, params object[] arguments)
        {
            MySqlCommand command = arguments.Length == 0
                ? new MySqlCommand(query, connection)
                : CreateCommand(query, arguments);
            MySqlDataReader dataReader;

            Dictionary<string, object> values = new Dictionary<string, object>();
            lock (actionMutex)
            {
                dataReader = command.ExecuteReader();
                if (dataReader.Read())
                {
                    object[] objects = new object[dataReader.FieldCount];
                    dataReader.GetValues(objects);
                    for (var i = 0; i < objects.Length; i++)
                    {
                        object value = objects[i];
                        if (value != null)
                        {
                            values.Add(dataReader.GetName(i), value);
                        }
                    }
                    dataReader.Close();
                }
                else
                {
                    dataReader.Close();
                    return null;
                }
            }
            
            
            return values;
        }
        public X First<X>(string query, params object[] arguments) where X: class
        {
            MySqlCommand command = arguments.Length == 0
                ? new MySqlCommand(query, connection)
                : CreateCommand(query, arguments);
            MySqlDataReader dataReader;
            X instance = (X) Activator.CreateInstance(typeof(X));
            lock (actionMutex)
            {
                dataReader = command.ExecuteReader();
                if (dataReader.Read())
                {
                    FillObject(dataReader, ref instance);
                    dataReader.Close();
                }
                else
                {
                    dataReader.Close();
                    return null;
                }
            }
            
            
            return instance;
        }

        public int DropTable(string tableName)
        {
            MySqlCommand mySqlCommand = new MySqlCommand("DROP TABLE @table", connection);
            mySqlCommand.Parameters.AddWithValue("@table", tableName);
            return mySqlCommand.ExecuteNonQuery();
        }
        
        public List<object[]> SelectRaw(string query, params object[] arguments)
        {
            MySqlCommand command = arguments.Length == 0
                ? new MySqlCommand(query, connection)
                : CreateCommand(query, arguments);
            List<object[]> list = new List<object[]>();
            
            lock (actionMutex)
            {
                MySqlDataReader dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    object[] objects = new object[dataReader.FieldCount];
                    dataReader.GetValues(objects);
                    
                    list.Add(objects);
                }
                dataReader.Close();
            }

            return list;
        }
        
        
        public List<Dictionary<string, object>> Select(string query, params object[] arguments)
        {
            MySqlCommand command = arguments.Length == 0
                ? new MySqlCommand(query, connection)
                : CreateCommand(query, arguments);
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            
            lock (actionMutex)
            {
                MySqlDataReader dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    Dictionary<string, object> objectData = new Dictionary<string, object>();
                    object[] objects = new object[dataReader.FieldCount];
                    dataReader.GetValues(objects);
                    for (var i = 0; i < objects.Length; i++)
                    {
                        object value = objects[i];
                        if (value != null)
                        {
                            objectData.Add(dataReader.GetName(i), value);
                        }
                    }
                    list.Add(objectData);
                }
                dataReader.Close();
            }

            return list;
        }
        
        
        public List<X> Select<X>(string query, params object[] arguments)
        {
            MySqlCommand command = arguments.Length == 0
                ? new MySqlCommand(query, connection)
                : CreateCommand(query, arguments);
            List<X> list = new List<X>();
            lock (actionMutex)
            {
                MySqlDataReader dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    
                    X instance = (X) Activator.CreateInstance(typeof(X));
                    FillObject(dataReader, ref instance);
                    list.Add(instance);
                }
                dataReader.Close();
            }

            return list;
        }

        public void FillObject<X>(MySqlDataReader dataReader, ref X instance)
        {
            Type type = instance.GetType();
            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                DBElement dbElement = fieldInfo.GetCustomAttribute<DBElement>();
                if (dbElement == null)
                {
                    continue;
                }

                switch (Type.GetTypeCode(fieldInfo.FieldType))
                {
                    case TypeCode.String:
                    {
                        fieldInfo.SetValue(instance, dataReader.GetString(dbElement.Name));
                        break;
                    }
                    case TypeCode.Int32:
                    {
                        fieldInfo.SetValue(instance, dataReader.GetInt32(dbElement.Name));
                        break;
                    }
                    case TypeCode.Int16:
                    {
                        fieldInfo.SetValue(instance, dataReader.GetInt16(dbElement.Name));
                        break;
                    }
                    case TypeCode.DateTime:
                    {
                        fieldInfo.SetValue(instance, dataReader.GetDateTime(dbElement.Name));
                        break;
                    }
                    case TypeCode.Int64:
                    {
                        fieldInfo.SetValue(instance, dataReader.GetInt64(dbElement.Name));
                        break;
                    }
                    case TypeCode.SByte:
                    {
                        fieldInfo.SetValue(instance, dataReader.GetSByte(dbElement.Name));
                        break;
                    }
                    case TypeCode.Double:
                    {
                        fieldInfo.SetValue(instance, dataReader.GetDouble(dbElement.Name));
                        break;
                    }
                    case TypeCode.Char:
                    {
                        fieldInfo.SetValue(instance, dataReader.GetChar(dbElement.Name));
                        break;
                    }
                    case TypeCode.Decimal:
                    {
                        fieldInfo.SetValue(instance, dataReader.GetDecimal(dbElement.Name));
                        break;
                    }
                }
            }
        }


        public MySqlCommand CreateCommand(string query, params object[] arguments)
        {
            // "SELECT * FROM test WHERE id=^ username=^ password=^"
            string realQuery = string.Empty;
            char lastChar = '-';
            int index = 0;
            foreach (char c in query.ToCharArray())
            {
                if (c == '^' && lastChar != '\\')
                {
                    realQuery += "@e" + index;
                    index++;
                    continue;
                }
                lastChar = c;
                realQuery += c;
            }


            MySqlCommand cmd = new MySqlCommand(realQuery, connection);

            if (arguments.Length > 0)
            {
                index = 0;
                foreach (object argument in arguments)
                {
                    cmd.Parameters.AddWithValue("@e" + index, argument);
                    index++;
                }
            }

            return cmd;
        }
        

    }
}
