﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IOT.Controller2
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class JONGOS_DBEntities : DbContext
    {
        public JONGOS_DBEntities()
            : base("name=JONGOS_DBEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceState> DeviceStates { get; set; }
        public DbSet<Module> Modules { get; set; }
    }
}
