namespace WebApplication1
{
    using Parliament.Ontology.Code;
    using System.Web.OData.Builder;

    /// <summary>
    /// A custom OData model builder that doesn't fail on self-references
    /// </summary>
    public class Builder : ODataModelBuilder
    {
        public Builder()
        {
            AddEntityTypes();
            AddEntitySets();
        }

        private void AddEntityTypes()
        {
            var iHouseSeat = this.EntityType<IHouseSeat>();
            iHouseSeat.HasKey(x => x.HouseSeatName);

            var iIncumbency = this.EntityType<IIncumbency>();
            iIncumbency.HasKey(x => x.IncumbencyStartDate);

            var iSeatIncumbency = this.EntityType<ISeatIncumbency>();
            iSeatIncumbency.DerivesFrom<IIncumbency>();
            iSeatIncumbency.HasOptional(x => x.SeatIncumbencyHasHouseSeat);
            iSeatIncumbency.HasMany(x => x.SeatIncumbencyHasParliamentPeriod);

            var iParliamentPeriod = this.EntityType<IParliamentPeriod>();
            iParliamentPeriod.HasKey(x => x.ParliamentPeriodNumber);
            iParliamentPeriod.Property(x => x.ParliamentPeriodStartDate);
            iParliamentPeriod.HasOptional(x => x.ParliamentPeriodHasImmediatelyPreviousParliamentPeriod);
            iParliamentPeriod.HasMany(x => x.ParliamentPeriodHasImmediatelyFollowingParliamentPeriod);
            iParliamentPeriod.HasMany(x => x.ParliamentPeriodHasSeatIncumbency);
        }

        private void AddEntitySets()
        {
            this.EntitySet<IHouseSeat>("HouseSeats");
            this.EntitySet<IIncumbency>("Incumbencies");
            this.EntitySet<ISeatIncumbency>("SeatIncumbencies");
            this.EntitySet<IParliamentPeriod>("ParliamentPeriods");
        }
    }
}