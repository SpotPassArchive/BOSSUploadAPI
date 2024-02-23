using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;
using System;

namespace BOSSUploadAPI;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
	public void OnResourceExecuting(ResourceExecutingContext context)
	{
		IList<IValueProviderFactory> factories = context.ValueProviderFactories;

		factories.RemoveType<FormValueProviderFactory>();
		factories.RemoveType<FormFileValueProviderFactory>();
		factories.RemoveType<JQueryFormValueProviderFactory>();
	}

	public void OnResourceExecuted(ResourceExecutedContext context) { }
}
