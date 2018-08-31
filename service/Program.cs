/*---------------------------------------------------------------------------------------------
*  Copyright (c) Nicolas Jinchereau. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace ShowdownSoftware
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder(args)
                   .UseStartup<Startup>()
                   .Build()
                   .Run();
        }
    }
}
