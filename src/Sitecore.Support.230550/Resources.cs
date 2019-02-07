namespace Sitecore.Support
{
  using System;
  using System.CodeDom.Compiler;
  using System.Diagnostics;
  using System.Runtime.CompilerServices;

  internal class Resources
  {
    internal Resources()
    {
    }

    internal static string CacheEntryInvalidType
    {
      get
      {
        return "The cache entry was not of the correct type.";
      }
    }
  }
}