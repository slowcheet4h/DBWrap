using System;

namespace DBWrap
{
    public class table : Attribute
    {
        public string Name { get; private set; }
        
        public table(string name)
        {
            Name = name;
        }
        
    }
}