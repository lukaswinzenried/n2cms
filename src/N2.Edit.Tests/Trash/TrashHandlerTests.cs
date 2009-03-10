using System;
using NUnit.Framework;
using N2.Definitions;
using N2.Persistence;
using N2.Web;
using Rhino.Mocks;
using N2.Engine.MediumTrust;
using N2.Edit.Trash;

namespace N2.Edit.Tests.Trash
{
    [TestFixture]
    public class TrashHandlerTests : TrashTestBase
    {
        [Test]
        public void ThrownItem_IsMoved_ToTrashcan()
        {
            IDefinitionManager definitions = mocks.StrictMock<IDefinitionManager>();

            IPersister persister = mocks.StrictMock<IPersister>();
            Expect.Call(persister.Get(1)).Return(root).Repeat.Any();
            Expect.Call(delegate { persister.Save(item); }).Repeat.Any();
		    
            mocks.ReplayAll();

            TrashHandler th = new TrashHandler(persister, definitions, host);
            th.Throw(item);

            Assert.AreEqual(trash, item.Parent);

            mocks.VerifyAll();
        }

        [Test]
        public void ThrownItem_IsExpired()
        {
            TrashHandler th = CreateTrashHandler();
            th.Throw(item);

            Assert.Less(DateTime.Now.AddSeconds(-10), item.Expires);
        }

        [Test]
        public void ChildrenOf_ThrownItem_AreExpired()
        {
            TrashHandler th = CreateTrashHandler();

            var child1 = CreateItem<ThrowableItem>(5, "child1", item);
            var child2 = CreateItem<ThrowableItem>(6, "child2", child1);

            th.Throw(item);

            Assert.That(child1.Expires, Is.Not.Null);
            Assert.That(child2.Expires, Is.Not.Null);
            Assert.That(child1.Expires, Is.GreaterThan(DateTime.Now.AddSeconds(-10)));
            Assert.That(child2.Expires, Is.GreaterThan(DateTime.Now.AddSeconds(-10)));
        }

        [Test]
        public void ChildrenOf_RestoredItems_AreUnExpired()
        {
            TrashHandler th = CreateTrashHandler();

            var child1 = CreateItem<ThrowableItem>(5, "child1", item);
            var child2 = CreateItem<ThrowableItem>(6, "child2", child1);

            th.Throw(item);

            th.Restore(item);

            Assert.That(child1.Expires, Is.Null);
            Assert.That(child2.Expires, Is.Null);
        }

        [Test]
        public void ThrownItem_NameIsCleared()
        {
            TrashHandler th = CreateTrashHandler();
            th.Throw(item);

            Assert.AreNotEqual("item", item.Name);
        }

        [Test]
        public void ThrownItem_OldValues_AreStored_InDetailBag()
        {
            TrashHandler th = CreateTrashHandler();
            th.Throw(item);

            Assert.AreEqual("item", item[TrashHandler.FormerName]);
            Assert.AreEqual(root, item[TrashHandler.FormerParent]);
            Assert.IsNull(item[TrashHandler.FormerExpires]);
            Assert.Less(DateTime.Now.AddSeconds(-10), (DateTime)item[TrashHandler.DeletedDate]);
        }

        [Test]
        public void Throwing_IsIntercepted_InMediumTrust()
        {
            MediumTrustEngine engine = new MediumTrustEngine();

            engine.Initialize();
            using (engine.Persister)
            {
                engine.Resolve<N2.Installation.InstallationManager>().Install();
            }
            engine.SecurityManager.Enabled = false;

            ContentItem root = new ThrowableItem();
            root.Name = "root";

            ContentItem item = new ThrowableItem();
            item.Name = "bin's destiny";
            item.AddTo(root);

            engine.Persister.Save(root);
            engine.Resolve<IHost>().DefaultSite.RootItemID = root.ID;
            engine.Resolve<IHost>().DefaultSite.StartPageID = root.ID;

            engine.Persister.Delete(item);

            Assert.That(root.Children.Count, Is.EqualTo(1));
            Assert.That(root.Children[0], Is.TypeOf(typeof(TrashContainerItem)));
            Assert.That(root.Children[0].Children[0], Is.EqualTo(item));
        }

        [Test]
        public void ThrashHandler_Throw_WillInvokeEvents()
        {
            var definitions = mocks.Stub<IDefinitionManager>();

            IPersister persister = mocks.StrictMock<IPersister>();
            Expect.Call(persister.Get(1)).Return(root).Repeat.Any();
            Expect.Call(delegate { persister.Save(item); }).Repeat.Any();

            mocks.ReplayAll();

            TrashHandler th = new TrashHandler(persister, definitions, new Host(webContext, 1, 1));

            bool throwingWasInvoked = false;
            bool throwedWasInvoked = false;
            th.ItemThrowing += delegate { throwingWasInvoked = true; };
            th.ItemThrowed += delegate { throwedWasInvoked = true; };
            th.Throw(item);

            Assert.That(throwingWasInvoked);
            Assert.That(throwedWasInvoked);

            mocks.VerifyAll();
        }

        [Test]
        public void ThrashHandler_Throw_CanBeCancelled()
        {
            var definitions = mocks.Stub<IDefinitionManager>();

            IPersister persister = mocks.StrictMock<IPersister>();
            Expect.Call(persister.Get(1)).Return(root).Repeat.Any();
            Expect.Call(delegate { persister.Save(item); }).Repeat.Never();

            mocks.ReplayAll();

            TrashHandler th = new TrashHandler(persister, definitions, new Host(webContext, 1, 1));

            th.ItemThrowing += delegate(object sender, CancellableItemEventArgs args) { args.Cancel = true; };
            th.Throw(item);

            mocks.VerifyAll();
        }

        #region Helper methods

        private TrashHandler CreateTrashHandler()
        {
            IDefinitionManager definitions = MockDefinitions();
            IPersister persister = MockPersister(root, trash, item);
            Expect.Call(delegate { persister.Move(null, null); }).IgnoreArguments()
                .Do(new Action<ContentItem, ContentItem>(delegate(ContentItem source, ContentItem destination)
                                                             {
                                                                 source.AddTo(destination);
                                                             })).Repeat.Any();
			
            mocks.ReplayAll();

            return new TrashHandler(persister, definitions, host);
        }

        private IPersister MockPersister(ContentItem root, ContentItem trash, ContentItem item)
        {
            IPersister persister = mocks.StrictMock<IPersister>();
            Expect.Call(persister.Get(1)).Return(root).Repeat.Any();
            Expect.Call(delegate { persister.Save(item); }).Repeat.Any();
            return persister;
        }

        private IDefinitionManager MockDefinitions()
        {
            return mocks.StrictMock<IDefinitionManager>();
        }

        #endregion

    }
}