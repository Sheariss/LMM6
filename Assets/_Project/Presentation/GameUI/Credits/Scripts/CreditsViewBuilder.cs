using UnityEngine.UIElements;

namespace Atlas.Presentation.Credits
{
    public sealed class CreditsViewBuilder
    {
        private readonly CreditsView view;
        private readonly CreditsLibrary library;

        public CreditsViewBuilder(CreditsView view, CreditsLibrary library)
        {
            this.view = view;
            this.library = library;
        }

        public void Build()
        {
            VisualElement content = view.Content;
            content.Clear();

            if (library == null)
                return;

            AddHero(content);

            foreach (CreditsSection section in library.Sections)
                AddSection(content, section);

            AddEnding(content);
        }

        private void AddHero(VisualElement parent)
        {
            VisualElement hero = new();
            hero.AddToClassList("credits-hero");

            Label title = new(library.ProjectTitle);
            title.AddToClassList("credits-hero-title");

            Label titleAccent = new(library.ProjectSubtitle);
            titleAccent.AddToClassList("credits-hero-title");
            titleAccent.AddToClassList("credits-hero-title--accent");

            Label kicker = new(library.ProjectLabel);
            kicker.AddToClassList("credits-hero-kicker");

            Label institution = new(library.Institution);
            institution.AddToClassList("credits-hero-school");

            Label description = new(library.ProjectDescription);
            description.AddToClassList("credits-hero-detail");

            hero.Add(title);
            hero.Add(titleAccent);
            hero.Add(kicker);
            hero.Add(institution);
            hero.Add(description);
            parent.Add(hero);
        }

        private void AddSection(VisualElement parent, CreditsSection sectionData)
        {
            if (sectionData == null)
                return;

            VisualElement section = new();
            section.AddToClassList("credits-section");

            Label sectionTitle = new(sectionData.Title);
            sectionTitle.AddToClassList("credits-section-title");
            section.Add(sectionTitle);

            foreach (CreditsEntry entry in sectionData.Entries)
                AddEntry(section, entry);

            parent.Add(section);
        }

        private void AddEntry(VisualElement parent, CreditsEntry entryData)
        {
            if (entryData == null)
                return;

            VisualElement entry = new();
            entry.AddToClassList("credits-entry");
            entry.AddToClassList(GetEntryStyleClass(entryData.Style));

            Label role = new(entryData.Role);
            role.AddToClassList("credits-role");

            Label name = new(entryData.Name);
            name.AddToClassList("credits-name");

            entry.Add(role);
            entry.Add(name);
            parent.Add(entry);
        }

        private static string GetEntryStyleClass(CreditsEntryStyle style)
        {
            return style switch
            {
                CreditsEntryStyle.Featured => "credits-entry--featured",
                CreditsEntryStyle.Joke => "credits-entry--joke",
                CreditsEntryStyle.SpecialThanks => "credits-entry--special-thanks",
                _ => "credits-entry--standard"
            };
        }

        private void AddEnding(VisualElement parent)
        {
            VisualElement ending = new();
            ending.AddToClassList("credits-ending");

            Label title = new(library.EndingTitle);
            title.AddToClassList("credits-ending-title");

            Label subtitle = new(library.EndingSubtitle);
            subtitle.AddToClassList("credits-ending-subtitle");

            Label meta = new(library.EndingMeta);
            meta.AddToClassList("credits-ending-meta");

            ending.Add(title);
            ending.Add(subtitle);
            ending.Add(meta);
            parent.Add(ending);
        }
    }
}