using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BoroHFR.Helpers;

public static class MvcExtensions
{
    /// <summary>
    /// Finds the appropriate controllers
    /// </summary>
    /// <param name="partManager">The manager for the parts</param>
    /// <param name="controllerTypes">The controller types that are allowed. </param>
    public static void UseSpecificControllers(this ApplicationPartManager partManager, params Type[] controllerTypes)
    {
        partManager.FeatureProviders.Add(new InternalControllerFeatureProvider());
        partManager.ApplicationParts.RemoveAt(0);
        partManager.ApplicationParts.Add(new SelectedControllersApplicationParts(controllerTypes));
    }

    /// <summary>
    /// Excludes the specified controllers
    /// </summary>
    /// <param name="partManager">The manager for the parts</param>
    /// <param name="controllerTypes">The controller types that are allowed. </param>
    public static void ExcludeSpecificControllers(this ApplicationPartManager partManager, params Type[] controllerTypes)
    {
        partManager.FeatureProviders.RemoveAt(0);
        partManager.FeatureProviders.Add(new ExcludeSpecifiedControllersFeatureProvider(controllerTypes));
    }

    /// <summary>
    /// Only allow selected controllers
    /// </summary>
    /// <param name="mvcBuilder">The builder that configures mvc core</param>
    /// <param name="controllerTypes">The controller types that are allowed. </param>
    public static IMvcBuilder UseSpecificControllers(this IMvcBuilder mvcBuilder, params Type[] controllerTypes) => mvcBuilder.ConfigureApplicationPartManager(partManager => partManager.UseSpecificControllers(controllerTypes));

    /// <summary>
    /// Exclude selected controllers
    /// </summary>
    /// <param name="mvcBuilder">The builder that configures mvc core</param>
    /// <param name="controllerTypes">The controller types that are allowed. </param>
    public static IMvcBuilder ExcludeSpecificControllers(this IMvcBuilder mvcBuilder, params Type[] controllerTypes) => mvcBuilder.ConfigureApplicationPartManager(partManager => partManager.ExcludeSpecificControllers(controllerTypes));

    /// <summary>
    /// Only instantiates selected controllers, not all of them. Prevents application scanning for controllers. 
    /// </summary>
    private class SelectedControllersApplicationParts : ApplicationPart, IApplicationPartTypeProvider
    {
        public SelectedControllersApplicationParts()
        {
            Name = "SelectedControllersApplicationParts";
            Types = Enumerable.Empty<TypeInfo>();
        }

        public SelectedControllersApplicationParts(Type[] types) : this()
        {
            Types = types.Select(x => x.GetTypeInfo()).ToArray();
        }

        public override string Name { get; }

        public IEnumerable<TypeInfo> Types { get; }
    }

    /// <summary>
    /// Ensure that internal controllers are also allowed. The default ControllerFeatureProvider hides internal controllers, but this one allows it. 
    /// </summary>
    private class InternalControllerFeatureProvider : ControllerFeatureProvider
    {
        private const string ControllerTypeNameSuffix = "Controller";

        /// <summary>
        /// Determines if a given <paramref name="typeInfo"/> is a controller. The default ControllerFeatureProvider hides internal controllers, but this one allows it. 
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/> candidate.</param>
        /// <returns><code>true</code> if the type is a controller; otherwise <code>false</code>.</returns>
        protected override bool IsController(TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass)
            {
                return false;
            }

            if (typeInfo.IsAbstract)
            {
                return false;
            }

            if (typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            if (typeInfo.IsDefined(typeof(Microsoft.AspNetCore.Mvc.NonControllerAttribute)))
            {
                return false;
            }

            if (!typeInfo.Name.EndsWith(ControllerTypeNameSuffix, StringComparison.OrdinalIgnoreCase) &&
                       !typeInfo.IsDefined(typeof(Microsoft.AspNetCore.Mvc.ControllerAttribute)))
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Ensure that internal controllers are also allowed. The default ControllerFeatureProvider hides internal controllers, but this one allows it. 
    /// </summary>
    private class ExcludeSpecifiedControllersFeatureProvider : ControllerFeatureProvider
    {
        private readonly TypeInfo[] _excludedTypes;

        public ExcludeSpecifiedControllersFeatureProvider(Type[] types)
        {
            _excludedTypes = types.Select(x=>x.GetTypeInfo()).ToArray();
        }

        private const string ControllerTypeNameSuffix = "Controller";

        /// <summary>
        /// Determines if a given <paramref name="typeInfo"/> is a controller. The default ControllerFeatureProvider hides internal controllers, but this one allows it. 
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/> candidate.</param>
        /// <returns><code>true</code> if the type is a controller; otherwise <code>false</code>.</returns>
        protected override bool IsController(TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass)
            {
                return false;
            }

            if (typeInfo.IsAbstract)
            {
                return false;
            }

            if (typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            if (typeInfo.IsDefined(typeof(Microsoft.AspNetCore.Mvc.NonControllerAttribute)))
            {
                return false;
            }

            if (!typeInfo.Name.EndsWith(ControllerTypeNameSuffix, StringComparison.OrdinalIgnoreCase) &&
                       !typeInfo.IsDefined(typeof(Microsoft.AspNetCore.Mvc.ControllerAttribute)))
            {
                return false;
            }

            if (_excludedTypes.Contains(typeInfo))
            {
                return false;
            }

            return true;
        }
    }
}