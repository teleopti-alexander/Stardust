﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Manager.Integration.Test.WPF
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class ManagerDbEntities : DbContext
    {
        public ManagerDbEntities()
            : base("name=ManagerDbEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<JobDefinition> JobDefinitions { get; set; }
        public virtual DbSet<JobHistory> JobHistories { get; set; }
        public virtual DbSet<JobHistoryDetail> JobHistoryDetails { get; set; }
        public virtual DbSet<Logging> Loggings { get; set; }
        public virtual DbSet<WorkerNode> WorkerNodes { get; set; }
    }
}
