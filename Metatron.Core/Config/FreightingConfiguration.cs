using System;

namespace Metatron.Core.Config
{
    /// <summary>
    /// Hold any freighting-related variables
    /// </summary>
    public sealed class FreightingConfiguration : ConfigurationBase
    {
        static readonly string FREIGHTER_MODE = "FreighterMode",
                               PICKUP_TYPE = "PickupType",
                               PICKUP_NAME = "PickupName",
                               PICKUP_ID = "PickupID",
                               DROPOFF_TYPE = "DropoffType",
                               DROPOFF_NAME = "DropoffName",
                               DROPOFF_ID = "DropoffID",
                               DROPOFF_FOLDER = "DropoffFolder";

        public FreighterModes FreighterMode
        {
            get { return (FreighterModes)GetInt32Property(FREIGHTER_MODE); }
            set { SetInt32Property(FREIGHTER_MODE, (Int32)value); } 
        }

        public LocationTypes PickupType
        {
            get { return (LocationTypes)GetInt32Property(PICKUP_TYPE); }
            set { SetInt32Property(PICKUP_TYPE, (Int32)value); }
        }
        public string PickupName
        {
            get { return GetStringProperty(PICKUP_NAME); }
            set { SetStringProperty(PICKUP_NAME, value); }
        }
        public Int64 PickupID
        {
            get { return GetInt64Property(PICKUP_ID); }
            set { SetInt64Property(PICKUP_ID, value); }
        }
        public LocationTypes LocationType
        {
            get { return (LocationTypes) GetInt32Property(DROPOFF_TYPE); }
            set { SetInt32Property(DROPOFF_TYPE, (Int32) value); }
        }
        public string DropoffName
        {
            get { return GetStringProperty(DROPOFF_NAME); }
            set { SetStringProperty(DROPOFF_NAME, value); }
        }
        public Int64 DropoffID
        {
            get { return GetInt64Property(DROPOFF_ID); }
            set { SetInt64Property(DROPOFF_ID, value); }
        }
        public int DropoffFolder
        {
            get { return GetInt32Property(DROPOFF_FOLDER); }
            set { SetInt32Property(DROPOFF_FOLDER, value); }
        }

        public FreightingConfiguration()
        {
            AddDefaultConfigProperties();
        }

        public override void AddDefaultConfigProperties()
        {
            AddDefaultConfigProperty(new ConfigProperty(FREIGHTER_MODE, (Int32)FreighterModes.PointToPoint));
            AddDefaultConfigProperty(new ConfigProperty(PICKUP_TYPE, (Int32)LocationTypes.Station));
            AddDefaultConfigProperty(new ConfigProperty(PICKUP_NAME, string.Empty));
            AddDefaultConfigProperty(new ConfigProperty(PICKUP_ID, -1));
            AddDefaultConfigProperty(new ConfigProperty(DROPOFF_TYPE, (Int32)LocationTypes.Station));
            AddDefaultConfigProperty(new ConfigProperty(DROPOFF_NAME, string.Empty));
            AddDefaultConfigProperty(new ConfigProperty(DROPOFF_ID, -1));
            AddDefaultConfigProperty(new ConfigProperty(DROPOFF_FOLDER, 1));
        }
    }
}