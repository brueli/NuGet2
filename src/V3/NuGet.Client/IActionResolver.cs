﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public interface IActionResolver
    {
        Task<IEnumerable<PackageActionDescription>> ResolveActions(
            PackageActionType action, 
            PackageIdentity target,
            ResolverContext context);
    }
}