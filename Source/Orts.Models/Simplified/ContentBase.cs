﻿using GetText;

namespace Orts.Models.Simplified
{
    public abstract class ContentBase
    {
        internal static readonly ICatalog catalog = CatalogManager.Catalog;

        protected const string Unknown = "unknown";
    }
}
