using System;

namespace DBWrap
{
    public class DBElement : Attribute
    {
        public string Name { get; private set; }


        public DBElement(string name)
        {
            Name = name;
        }

    }
}