/*---------------------------------------------------------------------------------------------
*  Copyright (c) Nicolas Jinchereau. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System;
using System.ServiceModel.Activation;
using System.Web.Routing;

namespace ShowdownSoftware
{
    public class Global : System.Web.HttpApplication 
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            RouteTable.Routes.Add(new ServiceRoute("scores", new WebServiceHostFactory(), typeof(ScoreService)));
        }
    }
}
