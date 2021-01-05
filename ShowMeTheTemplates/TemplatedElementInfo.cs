using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace ShowMeTheTemplates
{
    internal class TemplatedElementInfo
    {
        #region Private Fields

        private readonly Type elementType;
        private readonly IEnumerable<PropertyInfo> templatedProperties;

        #endregion Private Fields

        #region Public Constructors

        public TemplatedElementInfo(Type elementType, IEnumerable<PropertyInfo> templatedProperties)
        {
            this.elementType = elementType;
            this.templatedProperties = templatedProperties;
        }

        #endregion Public Constructors

        #region Public Properties

        public Type ElementType
        {
            get { return elementType; }
        }

        public IEnumerable<PropertyInfo> TemplateProperties
        {
            get { return templatedProperties; }
        }

        #endregion Public Properties

        #region Public Methods

        public static IEnumerable<TemplatedElementInfo> GetTemplatedElements(Assembly assem)
        {
            Type frameworkTemplateType = typeof(FrameworkTemplate);

            foreach (Type type in assem.GetTypes())
            {
                if (type.IsAbstract) { continue; }
                if (type.ContainsGenericParameters) { continue; }
                if (type.GetConstructor(Array.Empty<Type>()) == null) { continue; }

                List<PropertyInfo> templatedProperties = new List<PropertyInfo>();
                foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (frameworkTemplateType.IsAssignableFrom(prop.PropertyType))
                    {
                        templatedProperties.Add(prop);
                    }
                }

                if (templatedProperties.Count == 0) { continue; }

                yield return new TemplatedElementInfo(type, templatedProperties);
            }
        }

        #endregion Public Methods
    }
}