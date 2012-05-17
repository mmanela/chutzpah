using System;
using System.Globalization;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.TestWindow.Extensibility.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace VS11.Plugin
{
    public static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider serviceProvider)
            where T:class
        {
            return serviceProvider.GetService<T>(typeof(T));
        }

        /// <summary>
        /// Helper method to do some exception handling when getting a service from the service provider
        /// </summary>
        public static T GetService<T>(this IServiceProvider serviceProvider, Type serviceType)
            where T:class
        {
            ValidateArg.NotNull(serviceProvider, "serviceProvider");
            ValidateArg.NotNull(serviceType, "serviceType");

            var serviceInstance = serviceProvider.GetService(serviceType) as T;
            if (serviceInstance == null)
            {
                throw new ArgumentException(string.Format("Service '{0}' not found", serviceType.Name));
            }

            return serviceInstance;
        }
    }
}
