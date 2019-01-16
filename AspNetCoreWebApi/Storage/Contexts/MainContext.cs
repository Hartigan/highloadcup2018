using System;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class MainContext
    {
        public EmailContext Emails { get; } = new EmailContext();

        public FirstNameContext FirstNames { get; } = new FirstNameContext();

        public LastNameContext LastNames { get; } = new LastNameContext();

        public PhoneContext Phones { get; } = new PhoneContext();

        public SexContext Sex { get; } = new SexContext();

        public CountryContext Countries { get; } = new CountryContext();

        public CityContext Cities { get; } = new CityContext();

        public StatusContext Statuses { get; } = new StatusContext();

        public JoinedContext Joined { get; } = new JoinedContext();

        public BirthContext Birth { get; } = new BirthContext();

        public InterestsContext Interests { get; } = new InterestsContext();

        public LikesContext Likes { get; } = new LikesContext();

        public PremiumContext Premiums { get; } = new PremiumContext();

        public void Compress()
        {
            Emails.Compress();
            FirstNames.Compress();
            LastNames.Compress();
            Phones.Compress();
            Sex.Compress();
            Countries.Compress();
            Cities.Compress();
            Statuses.Compress();
            Joined.Compress();
            Birth.Compress();
            Interests.Compress();
            Likes.Compress();
            Premiums.Compress();
        }

        public void InitNull(IdStorage ids)
        {
            Cities.InitNull(ids);
            Countries.InitNull(ids);
            Interests.InitNull(ids);
            FirstNames.InitNull(ids);
            LastNames.InitNull(ids);
            Phones.InitNull(ids);
            Premiums.InitNull(ids);
        }
    }
}