namespace Mega.Client
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using Api.Messages;

	public sealed class FilesystemSnapshot
	{
		public IReadOnlyCollection<CloudItem> AllItems
		{
			get { return new ReadOnlyCollection<CloudItem>(_items); }
		}

		public IReadOnlyCollection<CloudItem> Roots
		{
			get { return new ReadOnlyCollection<CloudItem>(_roots); }
		}

		public CloudItem Files { get; private set; }
		public CloudItem Shares { get; private set; }
		public CloudItem Contacts { get; private set; }
		public CloudItem Trash { get; private set; }
		public CloudItem Inbox { get; private set; }

		private readonly MegaClient _owner;

		private readonly List<CloudItem> _items = new List<CloudItem>();
		private readonly List<CloudItem> _roots = new List<CloudItem>();

		internal readonly List<CloudItem> _orphans = new List<CloudItem>();

		internal FilesystemSnapshot(MegaClient owner)
		{
			_owner = owner;
		}

		internal void AddItem(Item itemInfo)
		{
			var item = CloudItem.FromTemplate(itemInfo, _owner);

			_items.Add(item);

			// Link with parent if possible.
			if (item.ParentID != null)
			{
				item.Parent = AllItems.FirstOrDefault(i => i.ID == item.ParentID);

				if (item.Parent != null)
				{
					item.Parent.Children = item.Parent.Children.Add(item);
				}
				else
				{
					_orphans.Add(item);
				}
			}
			else
			{
				_roots.Add(item);
			}

			// Is this maybe a parent to any orphan item?
			foreach (var orphan in _orphans)
			{
				if (orphan.ParentID == item.ID)
				{
					orphan.Parent = item;
					item.Children.Add(orphan);
				}
			}

			// Remove any orhpans we de-orphaned.
			_orphans.RemoveAll(o => o.Parent != null);

			// Is it a special item?
			if (item.Type == ItemType.Files)
				Files = item;
			else if (item.Type == ItemType.Inbox)
				Inbox = item;
			else if (item.Type == ItemType.Trash)
				Trash = item;
		}
	}
}