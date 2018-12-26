using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ProjectClass.MVC
{
    /// <summary>
    /// 校验是否需要登录，权限验证
    /// </summary>
    public class CheckLoginAttribute : ActionFilterAttribute, IActionFilter
    {
        public CheckLoginAttribute() { IsCheck = true; }
        public bool IsCheck { get; set; }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (IsCheck)
            {
                filterContext.Result = new RedirectResult("Home/Login");
            }
        }
    }
}
