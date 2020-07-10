using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FluentAssertions;
using GitCommands;
using GitCommands.Git;
using GitUIPluginInterfaces;
using NSubstitute;
using NUnit.Framework;
using ResourceManager;
using ResourceManager.CommitDataRenders;

namespace ResourceManagerTests.CommitDataRenders
{
    [SetCulture("en-US")]
    [SetUICulture("en-US")]
    [TestFixture]
    public class CommitDataHeaderRendererIntegrationTests
    {
        private CommitData _data;
        private CommitDataHeaderRenderer _rendererTabs;
        private CommitDataHeaderRenderer _rendererSpaces;
        private GitRevision _parent1Rev;
        private GitRevision _parent2Rev;
        private GitRevision _child1Rev;
        private GitRevision _child2Rev;
        private GitRevision _child3Rev;
        private IGitRevisionProvider _revisionProvider;

        [SetUp]
        public void Setup()
        {
            var commitGuid = ObjectId.Random();
            var treeGuid = ObjectId.Random();
            var parentId1 = ObjectId.Random();
            var parentId2 = ObjectId.Random();
            var authorTime = DateTime.UtcNow.AddDays(-3);
            var commitTime = DateTime.UtcNow.AddDays(-2);

            _revisionProvider = Substitute.For<IGitRevisionProvider>();
            _parent1Rev = new GitRevision(parentId1) { Subject = "Parent1" };
            _parent2Rev = new GitRevision(parentId2) { Subject = "Parent2" };
            _revisionProvider.GetRevision(parentId1, shortFormat: true).Returns(_parent1Rev);
            _revisionProvider.GetRevision(parentId2, shortFormat: true).Returns(_parent2Rev);

            var childIds = new[]
            {
                ObjectId.Parse("3b6ce324e30ed7fda24483fd56a180c34a262202"),
                ObjectId.Parse("2a8788ff15071a202505a96f80796dbff5750ddf"),
                ObjectId.Parse("8e66fa8095a86138a7c7fb22318d2f819669831e")
            };
            _child1Rev = new GitRevision(childIds[0]) { Subject = "Child1" };
            _child2Rev = new GitRevision(childIds[1]) { Subject = "Child2" };
            _child3Rev = new GitRevision(childIds[2]) { Subject = "Child3" };
            _revisionProvider.GetRevision(childIds[0], shortFormat: true).Returns(_child1Rev);
            _revisionProvider.GetRevision(childIds[1], shortFormat: true).Returns(_child2Rev);
            _revisionProvider.GetRevision(childIds[2], shortFormat: true).Returns(_child3Rev);

            _data = new CommitData(commitGuid, treeGuid,
                new ReadOnlyCollection<ObjectId>(new List<ObjectId> { parentId1, parentId2 }),
                "John Doe (Acme Inc) <John.Doe@test.com>", authorTime,
                "Jane Doe <Jane.Doe@test.com>", commitTime,
                "\tI made a really neat change.\n\nNotes (p4notes):\n\tP4@547123") { ChildIds = childIds };

            _rendererTabs = new CommitDataHeaderRenderer(new TabbedHeaderLabelFormatter(), new DateFormatter(), new TabbedHeaderRenderStyleProvider(), new LinkFactory());
            _rendererSpaces = new CommitDataHeaderRenderer(new MonospacedHeaderLabelFormatter(), new DateFormatter(), new MonospacedHeaderRenderStyleProvider(), new LinkFactory());
        }

        [Test]
        public void Render_with_tabs_and_links()
        {
            var expectedHeader = "Author:			<a href='mailto:John.Doe@test.com'>John Doe (Acme Inc) &lt;John.Doe@test.com&gt;</a>" + Environment.NewLine +
                                 "Author date:	3 days ago (" + LocalizationHelpers.GetFullDateString(_data.AuthorDate) + ")" + Environment.NewLine +
                                 "Committer:		<a href='mailto:Jane.Doe@test.com'>Jane Doe &lt;Jane.Doe@test.com&gt;</a>" + Environment.NewLine +
                                 "Commit date:	2 days ago (" + LocalizationHelpers.GetFullDateString(_data.CommitDate) + ")" + Environment.NewLine +
                                 "Commit hash:	" + _data.ObjectId + Environment.NewLine +
                                 $"Children:		<a href='gitext://gotocommit/{_data.ChildIds[0]}'>{_data.ChildIds[0].ToShortString()}</a> {_child1Rev.Subject}{Environment.NewLine}" +
                                 $"				<a href='gitext://gotocommit/{_data.ChildIds[1]}'>{_data.ChildIds[1].ToShortString()}</a> {_child2Rev.Subject}{Environment.NewLine}" +
                                 $"				<a href='gitext://gotocommit/{_data.ChildIds[2]}'>{_data.ChildIds[2].ToShortString()}</a> {_child3Rev.Subject}{Environment.NewLine}" +
                                 $"Parents:		<a href='gitext://gotocommit/{_data.ParentIds[0]}'>{_data.ParentIds[0].ToShortString()}</a> {_parent1Rev.Subject}{Environment.NewLine}" +
                                 $"				<a href='gitext://gotocommit/{_data.ParentIds[1]}'>{_data.ParentIds[1].ToShortString()}</a> {_parent2Rev.Subject}";

            var result = _rendererTabs.Render(_data, true, _revisionProvider);

            result.Should().Be(expectedHeader);
        }

        [Test]
        public void Render_with_tabs_no_links()
        {
            var expectedHeader = "Author:			<a href='mailto:John.Doe@test.com'>John Doe (Acme Inc) &lt;John.Doe@test.com&gt;</a>" + Environment.NewLine +
                                 "Author date:	3 days ago (" + LocalizationHelpers.GetFullDateString(_data.AuthorDate) + ")" + Environment.NewLine +
                                 "Committer:		<a href='mailto:Jane.Doe@test.com'>Jane Doe &lt;Jane.Doe@test.com&gt;</a>" + Environment.NewLine +
                                 "Commit date:	2 days ago (" + LocalizationHelpers.GetFullDateString(_data.CommitDate) + ")" + Environment.NewLine +
                                 "Commit hash:	" + _data.ObjectId + Environment.NewLine +
                                 $"Children:		{_data.ChildIds[0].ToShortString()} {_child1Rev.Subject}{Environment.NewLine}" +
                                 $"				{_data.ChildIds[1].ToShortString()} {_child2Rev.Subject}{Environment.NewLine}" +
                                 $"				{_data.ChildIds[2].ToShortString()} {_child3Rev.Subject}{Environment.NewLine}" +
                                 $"Parents:		{_data.ParentIds[0].ToShortString()} {_parent1Rev.Subject}{Environment.NewLine}" +
                                 $"				{_data.ParentIds[1].ToShortString()} {_parent2Rev.Subject}";

            var result = _rendererTabs.Render(_data, false, _revisionProvider);

            result.Should().Be(expectedHeader);
        }

        [Test]
        public void Render_with_spaces_with_links()
        {
            var expectedHeader =
                "Author:      <a href='mailto:John.Doe@test.com'>John Doe (Acme Inc) &lt;John.Doe@test.com&gt;</a>" +
                Environment.NewLine +
                "Author date: 3 days ago (" + LocalizationHelpers.GetFullDateString(_data.AuthorDate) + ")" +
                Environment.NewLine +
                "Committer:   <a href='mailto:Jane.Doe@test.com'>Jane Doe &lt;Jane.Doe@test.com&gt;</a>" +
                Environment.NewLine +
                "Commit date: 2 days ago (" + LocalizationHelpers.GetFullDateString(_data.CommitDate) + ")" +
                Environment.NewLine +
                "Commit hash: " + _data.ObjectId + Environment.NewLine +
                $"Children:    <a href='gitext://gotocommit/{_data.ChildIds[0]}'>{_data.ChildIds[0].ToShortString()}</a> {_child1Rev.Subject}{Environment.NewLine}" +
                $"             <a href='gitext://gotocommit/{_data.ChildIds[1]}'>{_data.ChildIds[1].ToShortString()}</a> {_child2Rev.Subject}{Environment.NewLine}" +
                $"             <a href='gitext://gotocommit/{_data.ChildIds[2]}'>{_data.ChildIds[2].ToShortString()}</a> {_child3Rev.Subject}{Environment.NewLine}" +
                $"Parents:     <a href='gitext://gotocommit/{_data.ParentIds[0]}'>{_data.ParentIds[0].ToShortString()}</a> {_parent1Rev.Subject}{Environment.NewLine}" +
                $"             <a href='gitext://gotocommit/{_data.ParentIds[1]}'>{_data.ParentIds[1].ToShortString()}</a> {_parent2Rev.Subject}";

            var result = _rendererSpaces.Render(_data, true, _revisionProvider);

            result.Should().Be(expectedHeader);
        }

        [Test]
        public void Render_with_spaces_no_links()
        {
            var expectedHeader =
                "Author:      <a href='mailto:John.Doe@test.com'>John Doe (Acme Inc) &lt;John.Doe@test.com&gt;</a>" +
                Environment.NewLine +
                "Author date: 3 days ago (" + LocalizationHelpers.GetFullDateString(_data.AuthorDate) + ")" +
                Environment.NewLine +
                "Committer:   <a href='mailto:Jane.Doe@test.com'>Jane Doe &lt;Jane.Doe@test.com&gt;</a>" +
                Environment.NewLine +
                "Commit date: 2 days ago (" + LocalizationHelpers.GetFullDateString(_data.CommitDate) + ")" +
                Environment.NewLine +
                "Commit hash: " + _data.ObjectId + Environment.NewLine +
                $"Children:    {_data.ChildIds[0].ToShortString()} {_child1Rev.Subject}{Environment.NewLine}" +
                $"             {_data.ChildIds[1].ToShortString()} {_child2Rev.Subject}{Environment.NewLine}" +
                $"             {_data.ChildIds[2].ToShortString()} {_child3Rev.Subject}{Environment.NewLine}" +
                $"Parents:     {_data.ParentIds[0].ToShortString()} {_parent1Rev.Subject}{Environment.NewLine}" +
                $"             {_data.ParentIds[1].ToShortString()} {_parent2Rev.Subject}";

            var result = _rendererSpaces.Render(_data, false, _revisionProvider);

            result.Should().Be(expectedHeader);
        }

        [Test]
        public void RenderPlain_with_tabs()
        {
            var expectedHeader = "Author:			John Doe (Acme Inc) <John.Doe@test.com>" + Environment.NewLine +
                                 "Author date:	3 days ago (" + LocalizationHelpers.GetFullDateString(_data.AuthorDate) + ")" + Environment.NewLine +
                                 "Committer:		Jane Doe <Jane.Doe@test.com>" + Environment.NewLine +
                                 "Commit date:	2 days ago (" + LocalizationHelpers.GetFullDateString(_data.CommitDate) + ")" + Environment.NewLine +
                                 "Commit hash:	" + _data.ObjectId;

            var result = _rendererTabs.RenderPlain(_data);

            result.Should().Be(expectedHeader);
        }

        [Test]
        public void RenderPlain_with_spaces()
        {
            var expectedHeader = "Author:      John Doe (Acme Inc) <John.Doe@test.com>" + Environment.NewLine +
                                 "Author date: 3 days ago (" + LocalizationHelpers.GetFullDateString(_data.AuthorDate) + ")" + Environment.NewLine +
                                 "Committer:   Jane Doe <Jane.Doe@test.com>" + Environment.NewLine +
                                 "Commit date: 2 days ago (" + LocalizationHelpers.GetFullDateString(_data.CommitDate) + ")" + Environment.NewLine +
                                 "Commit hash: " + _data.ObjectId;

            var result = _rendererSpaces.RenderPlain(_data);

            result.Should().Be(expectedHeader);
        }
    }
}
