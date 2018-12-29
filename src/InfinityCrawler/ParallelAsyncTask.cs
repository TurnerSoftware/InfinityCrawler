using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InfinityCrawler
{
	internal static class ParallelAsyncTask
	{
		public static async Task For<TModel>(
			IEnumerable<TModel> items, 
			Func<TModel, ConcurrentQueue<TModel>, Task> action, 
			int numberOfParallelAsyncTasks = 5
		)
		{
			var itemsToProcess = new ConcurrentQueue<TModel>(items);
			var activeTasks = new ConcurrentDictionary<Task, byte>();

			while (activeTasks.Count > 0 || !itemsToProcess.IsEmpty)
			{
				while (!itemsToProcess.IsEmpty)
				{
					if (itemsToProcess.TryDequeue(out var item))
					{
						var task = action(item, itemsToProcess);
						activeTasks.TryAdd(task, 0);

						if (activeTasks.Count == numberOfParallelAsyncTasks)
						{
							break;
						}
					}
				}

				await Task.WhenAny(activeTasks.Keys).ConfigureAwait(false);
				
				var completedTasks = activeTasks.Keys.Where(t => t.IsCompleted);
				foreach (var completedTask in completedTasks)
				{
					activeTasks.TryRemove(completedTask, out var val);
				}
			}
		}
	}
}
