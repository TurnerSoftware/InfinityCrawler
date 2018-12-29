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
		public static async Task For<TModel, TContext>(
			IEnumerable<TModel> items, 
			Func<ItemContext<TModel, TContext>, ConcurrentQueue<ItemContext<TModel, TContext>>, Task> action, 
			int numberOfParallelAsyncTasks = 5
		)
		{
			var itemsToProcess = new ConcurrentQueue<ItemContext<TModel, TContext>>(
				items.Select(i => new ItemContext<TModel, TContext>(i)).ToArray()
			);

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

		public class ItemContext<TModel, TContext>
		{
			public TModel Model { get; set; }
			public TContext Context { get; set; }

			public ItemContext() { }

			public ItemContext(TModel model)
			{
				Model = model;
			}
		}
	}
}
