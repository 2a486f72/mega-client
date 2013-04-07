namespace Mega.Client
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using Api.Messages;

	/// <summary>
	/// A snapshot of the current account's cloud filesystem at some point in time; includes all items the current account can access.
	/// Any operations you perform perform do not update the snapshot - you will need to obtain a new snapshot to see the changes.
	/// </summary>
	/// <remarks>
	/// Some items are excluded from the hierarchly - namely orphaned items or those the current account has no key for.
	/// </remarks>
	public sealed class FilesystemSnapshot
	{
		/// <summary>
		/// All items in the filesystem, except any excluded items.
		/// </summary>
		public IReadOnlyCollection<CloudItem> AllItems
		{
			get { return new ReadOnlyCollection<CloudItem>(_items); }
		}

		/// <summary>
		/// Roots of the cloud filesystem, except shares which are a separate set of roots.
		/// </summary>
		public IReadOnlyCollection<CloudItem> Roots
		{
			get { return new ReadOnlyCollection<CloudItem>(_roots); }
		}

		/// <summary>
		/// Gets the "Files" root.
		/// </summary>
		public CloudItem Files { get; private set; }

		/// <summary>
		/// Gets the "Trash" root.
		/// </summary>
		public CloudItem Trash { get; private set; }

		/// <summary>
		/// Gets the "Inbox" root.
		/// </summary>
		public CloudItem Inbox { get; private set; }

		/// <summary>
		/// Every share accessible to the current account acts as a separate root.
		/// This does not include shares owned by the current account, which are a separate concept and not a part of your filesystem.
		/// </summary>
		public IReadOnlyCollection<CloudItem> Shares { get { return new ReadOnlyCollection<CloudItem>(_shares);} }

		private readonly MegaClient _owner;

		private readonly List<CloudItem> _items = new List<CloudItem>();
		private readonly List<CloudItem> _roots = new List<CloudItem>();
		private readonly List<CloudItem> _shares = new List<CloudItem>();

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
			if (item.IsShareRoot)
			{
				_shares.Add(item);

				// Pretend it has no parent, since share root parents are going to be external to the current filesystem.
				item.ParentID = null;
			}
			else if (item.ParentID != null)
			{
				item.Parent = AllItems.FirstOrDefault(i => i.ID == item.ParentID);

				if (item.Parent != null)
				{
					item.Parent.Children = item.Parent.Children.Add(item);
				}
				else
				{
					// The parent is not a part of the current filesystem. Weird, huh?
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