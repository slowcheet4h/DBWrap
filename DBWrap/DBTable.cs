using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DBWrap
{
    public class DBTable
    {

        public int Remove(DatabaseClient databaseClient)
        {
            Tuple<string, object> identifier = IdentifierData();
            if (identifier == null)
            {
                throw new Exception("Identifier not found");
            }
            
            return databaseClient.Execute($"DELETE FROM {Name} WHERE {identifier.Item1}=^ LIMIT 1", identifier.Item2);
        }

        public int Insert(DatabaseClient client)
        {
            Tuple<DBElement, object> identifier = null;
            string sql = $"INSERT INTO {Name} (";
            Dictionary<string, object> values = new Dictionary<string, object>();

            int index = 0;
            int last = GetType().GetFields().Length - 1;
            foreach (FieldInfo fieldInfo in GetType().GetFields())
            {
                DBElement dbElement = fieldInfo.GetCustomAttribute<DBElement>();
                if (fieldInfo.GetCustomAttribute<PK>() != null)
                {
                    identifier = new Tuple<DBElement, object>(dbElement,
                        fieldInfo.GetValue(this));
                }
                else
                {
                    sql += dbElement.Name + (index != last ? "," : string.Empty);
                    values.Add(dbElement.Name, fieldInfo.GetValue(this));
                }

                index++;
            }

            sql += ") VALUES (";


            object[] arguments = new object[values.Count + 1];
            index = 0;
            foreach (KeyValuePair<string, object> pair in values)
            {
                sql += "^" + (index != values.Count -1 ? "," : ")");
                arguments[index++] = pair.Value;
            }

            arguments[values.Count] = identifier.Item2;
        
            return client.Execute(sql, arguments);
        }
        
        public int Update(DatabaseClient client)
        {
            Tuple<DBElement, object> identifier = null;
            string sql = $"UPDATE {Name} SET ";
            Dictionary<string, object> values = new Dictionary<string, object>();

            int index = 0;
            int last = GetType().GetFields().Length - 1;
            foreach (FieldInfo fieldInfo in GetType().GetFields())
            {
                DBElement dbElement = fieldInfo.GetCustomAttribute<DBElement>();
                if (fieldInfo.GetCustomAttribute<PK>() != null)
                {
                    identifier = new Tuple<DBElement, object>(dbElement,
                        fieldInfo.GetValue(this));
                }
                else
                {
                    sql += dbElement.Name + "=^" + (index != last ? "," : string.Empty);
                    values.Add(dbElement.Name, fieldInfo.GetValue(this));
                }

                index++;
            }
            
            if (identifier == null)
            {
                throw new Exception("Primary key not found [PK]");
            }

            object[] arguments = new object[values.Count + 1];
            index = 0;
            foreach (KeyValuePair<string, object> pair in values)
            {
                
                arguments[index++] = pair.Value;
            }

            arguments[values.Count] = identifier.Item2;
        
            return client.Execute(sql + " WHERE " + identifier.Item1.Name + "=^", arguments);
        }

      

        public Tuple<string, object> IdentifierData()
        {
            foreach (FieldInfo fieldInfo in GetType().GetFields())
            {
                if (fieldInfo.GetCustomAttribute<PK>() != null)
                {
                    return new Tuple<string, object>(fieldInfo.GetCustomAttribute<DBElement>().Name, fieldInfo.GetValue(this));
                }
            }

            return null;
        }
        
        public string Name
        {
            get
            {
                return GetType().GetCustomAttribute<table>().Name;
            }
        }
    }
}