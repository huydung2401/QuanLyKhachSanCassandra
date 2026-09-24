using System.Web.Mvc;
using System.Web.Routing;

namespace QuanLyKhachSan
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // =====================================================
            // ADMIN ROUTE
            // /Admin/Room
            // /Admin/Room/Index
            // =====================================================

            routes.MapRoute(
                name: "Admin",
                url: "Admin/{controller}/{action}/{id}",
                defaults: new
                {
                    controller = "Dashboard",
                    action = "Index",
                    id = UrlParameter.Optional
                },
                namespaces: new[]
                {
                    "QuanLyKhachSan.Controllers.Admin"
                }
            );

            // =====================================================
            // DEFAULT / USER ROUTE
            // =====================================================

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new
                {
                    controller = "UserHome",
                    action = "Index",
                    id = UrlParameter.Optional
                },
                namespaces: new[]
                {
                    "QuanLyKhachSan.Controllers.User"
                }
            );
        }
    }
}