using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;

namespace CinderLang
{
    public static class BackendManager
    {
        public static Type[] GetBackends()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes(typeof(BackendAttribute), false).Any());

            foreach (var type in types)
            {
                if (!typeof(IBuilder).IsAssignableFrom(type))
                    ErrorManager.Throw(ErrorType.Backend,"The 'Backend' attribute can only be assigned to classes that implement 'IBuilder'");
            }

            return types.ToArray();
        }

        public static Type GetBackend(string name)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Select(t => new
                {
                    Type = t,
                    Attr = (BackendAttribute)Attribute.GetCustomAttribute(t, typeof(BackendAttribute))!
                })
                .Where(x => x.Attr != null && x.Attr.BackendName.ToLower() == name.ToLower())
                .Select(x => x.Type)
                .SingleOrDefault();

            if (type == null)
                ErrorManager.Throw(ErrorType.Backend, $"The '{name}' backend does not exist");

            if (!typeof(IBuilder).IsAssignableFrom(type))
                ErrorManager.Throw(ErrorType.Backend, "The 'Backend' attribute can only be assigned to classes that implement 'IBuilder'");

            return type!;
        }
    }
}
