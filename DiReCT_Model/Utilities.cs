﻿using System.Reflection;

namespace System
{
    public static class Utilities
    {
        /// <summary>
        /// Database records tables inherit the observation records,
        /// so the observation record must be converted to the type 
        /// of the record table before it is stored in the database.
        /// 
        /// Super class objects convert to son object.
        /// In the concept of oop,
        /// 
        /// super class object can not be transformed into Sub Class object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Father"></param>
        /// <returns></returns>
        public static T ConvertToSon<T>(this object Father)
        {
            // Get father object type.
            Type FatherType = Father.GetType();

            // Create a son object.
            object SonObject = Activator.CreateInstance(typeof(T));

            // Get father object all field information to array.
            FieldInfo[] fields = FatherType.GetFields(BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public);

            // Copy all field value to son object.
            foreach (var q in fields)
                q.SetValue(SonObject, q.GetValue(Father));

            return (T)SonObject;

        }
    }
}
