using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace VS11.Plugin
{
	public static class VsSolutionHelper
	{
		public static IEnumerable<IVsHierarchy> EnumerateLoadedProjects(this IVsSolution solution, __VSENUMPROJFLAGS enumFlags)
		{
			Guid prjType = Guid.Empty;
			IEnumHierarchies ppHier;

			int hr = solution.GetProjectEnum((uint)enumFlags, ref prjType, out ppHier);
			if (ErrorHandler.Succeeded(hr) && ppHier != null)
			{
				uint fetched = 0;
				IVsHierarchy[] hierarchies = new IVsHierarchy[1];
				while (ppHier.Next(1, hierarchies, out fetched) == VSConstants.S_OK)
				{
					yield return hierarchies[0];
				}
			}
		}
		public static IEnumerable<string> GetProjectItems(IVsProject project)
		{
			// Each item in VS OM is IVSHierarchy. 
			IVsHierarchy hierarchy = (IVsHierarchy)project;

			return GetProjectItems(hierarchy, VSConstants.VSITEMID_ROOT);
		}
		public static IEnumerable<string> GetProjectItems(IVsHierarchy project, uint itemId)
		{
			object pVar = GetPropertyValue((int)__VSHPROPID.VSHPROPID_FirstChild, itemId, project);

			uint childId = GetItemId(pVar);
			while (childId != VSConstants.VSITEMID_NIL)
			{
				string childPath = GetCanonicalName(childId, project);
				yield return childPath;

				foreach (var childNodePath in GetProjectItems(project, childId)) yield return childNodePath;

				pVar = GetPropertyValue((int)__VSHPROPID.VSHPROPID_NextSibling, childId, project);
				childId = GetItemId(pVar);
			}
		}
		public static uint GetItemId(object pvar)
		{
			if (pvar == null) return VSConstants.VSITEMID_NIL;
			if (pvar is int) return (uint)(int)pvar;
			if (pvar is uint) return (uint)pvar;
			if (pvar is short) return (uint)(short)pvar;
			if (pvar is ushort) return (uint)(ushort)pvar;
			if (pvar is long) return (uint)(long)pvar;
			return VSConstants.VSITEMID_NIL;
		}
		public static object GetPropertyValue(int propid, uint itemId, IVsHierarchy vsHierarchy)
		{
			if (itemId == VSConstants.VSITEMID_NIL)
			{
				return null;
			}

			try
			{
				object o;
				ErrorHandler.ThrowOnFailure(vsHierarchy.GetProperty(itemId, propid, out o));

				return o;
			}
			catch (System.NotImplementedException)
			{
				return null;
			}
			catch (System.Runtime.InteropServices.COMException)
			{
				return null;
			}
			catch (System.ArgumentException)
			{
				return null;
			}
		}
		/// <summary>
		/// Get the canonical name
		/// </summary>
		public static string GetCanonicalName(uint itemId, IVsHierarchy hierarchy)
		{
			string strRet = string.Empty;
			int hr = hierarchy.GetCanonicalName(itemId, out strRet);

			if (hr == VSConstants.E_NOTIMPL)
			{
				// Special case E_NOTIMLP to avoid perf hit to throw an exception.
				return string.Empty;
			}
			else
			{
				try
				{
					ErrorHandler.ThrowOnFailure(hr);
				}
				catch (System.Runtime.InteropServices.COMException)
				{
					strRet = string.Empty;
				}

				// This could be in the case of S_OK, S_FALSE, etc.
				return strRet;
			}
		}
	}

}
