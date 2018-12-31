using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InfinityCrawler.TaskHandlers
{
	public interface ITaskHandler
	{
		Task For<TModel>(
			IEnumerable<TModel> items,
			Func<TModel, ConcurrentQueue<TModel>, Task> action,
			TaskHandlerOptions options
		);
	}
}
