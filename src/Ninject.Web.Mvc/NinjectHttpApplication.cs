using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using Ninject.Planning.Bindings;
using System.Linq;

namespace Ninject.Web.Mvc
{
    /// <summary>
    /// A controller factory that creates <see cref="IController"/>s via Ninject.
    /// </summary>
    public class NinjectControllerFactory : DefaultControllerFactory
    {
        /// <summary>
        /// Gets the kernel that will be used to create controllers.
        /// </summary>
        public IKernel Kernel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NinjectControllerFactory"/> class.
        /// </summary>
        /// <param name="kernel">The kernel that should be used to create controllers.</param>
        public NinjectControllerFactory(IKernel kernel)
        {
            Kernel = kernel;
        }

        /// <summary>
        /// Creates the controller with the specified name.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="controllerName">Name of the controller.</param>
        /// <returns>The created controller.</returns>
        public override IController CreateController(RequestContext requestContext, string controllerName)
        {
            IController controller = null;

            // if the route provides a namespace resolve the controller by controllerName and namespace
            object routeNamespacesObj;
            if (requestContext != null && requestContext.RouteData.DataTokens.TryGetValue("Namespaces", out routeNamespacesObj))
            {
                IEnumerable<string> routeNamespaces = routeNamespacesObj as IEnumerable<string>;
                if (routeNamespaces != null && routeNamespaces.Any())
                    controller = Kernel.TryGet<IController>(ctx => ctx.Name == controllerName.ToLowerInvariant() && IsControllerInNamespace(ctx, routeNamespaces.FirstOrDefault()));
            }

            // otherwise use the controllerName to resolve
            if (controller == null)
                controller = Kernel.TryGet<IController>(controllerName.ToLowerInvariant());

            if (controller == null)
                return base.CreateController(requestContext, controllerName);

            var standardController = controller as Controller;

            if (standardController != null)
                standardController.ActionInvoker = new NinjectActionInvoker(Kernel);

            return controller;
        }

        /// <summary>
        /// Releases the specified controller.
        /// </summary>
        /// <param name="controller">The controller to release.</param>
        public override void ReleaseController(IController controller) { }

        private static bool IsControllerInNamespace(IBindingMetadata binding, string @namespace)
        {
            if (binding != null && !string.IsNullOrEmpty(@namespace) && binding.Has("Namespace"))
                return binding.Get<string>("Namespace").ToLowerInvariant() == @namespace.ToLowerInvariant();
            return false;
        }
    }
}