using Microsoft.EntityFrameworkCore;
using TeamFlowAPI.Models.Entities; // Adjust namespace based on your structure

namespace TeamFlowAPI.Infrastructure.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    
    public DbSet<User> Users { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationUser> OrganizationUsers { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TaskAssignment> TaskAssignments { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<PlatformAdmin> PlatformAdmins { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Invitation> Invitations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
         // here i configure my fluent api by telling EF Core the rules of my existing database
         /* 
            Even though your database already exists, EF Core needs to understand:

                "This is how my tables are named"

                "These are my special constraints"

                "This is how my relationships work"
         */  

        // table renaming to match my database tables
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Organization>().ToTable("organizations");
        modelBuilder.Entity<OrganizationUser>().ToTable("organization_users");
        modelBuilder.Entity<Team>().ToTable("teams");
        modelBuilder.Entity<TeamMember>().ToTable("team_members");
        modelBuilder.Entity<Project>().ToTable("projects");
        modelBuilder.Entity<TaskItem>().ToTable("tasks");
        modelBuilder.Entity<TaskAssignment>().ToTable("task_assignees");
        modelBuilder.Entity<Comment>().ToTable("comments");
        modelBuilder.Entity<PlatformAdmin>().ToTable("platform_admins");
        modelBuilder.Entity<ActivityLog>().ToTable("activity_logs");
        modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");
        modelBuilder.Entity<Attachment>().ToTable("attachments");
        modelBuilder.Entity<Invitation>().ToTable("invitations");
        

        // unique constraints
        modelBuilder.Entity<OrganizationUser>()
        .HasIndex(ou => new { ou.OrgId, ou.UserId })
        .IsUnique();

        modelBuilder.Entity<TeamMember> ()
        .HasIndex(ou => new { ou.TeamId,  ou.UserId})
        .IsUnique();

        modelBuilder.Entity<TaskAssignment> ()
        .HasIndex(ou => new { ou.TaskId, ou.UserId})
        .IsUnique();

        //Delete Behaviors 
        // When organization is deleted, delete all its users
        modelBuilder.Entity<Organization>()
            .HasMany(o => o.OrganizationUsers)
            .WithOne(ou => ou.Organization)
            .OnDelete(DeleteBehavior.Cascade);

        // When user is deleted, delete all their organization memberships
        modelBuilder.Entity<User>()
            .HasMany(u => u.OrganizationUsers)
            .WithOne(ou => ou.User)
            .OnDelete(DeleteBehavior.Cascade);

        // SET NULL (preserve data)
        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.ParentTask)
            .WithMany(t => t.Subtasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        //configuration for PlatformAdmin
        modelBuilder.Entity<PlatformAdmin>()
            .HasOne(u => u.User)         
            .WithMany()                     
            .HasForeignKey(u => u.UserId) 
            .IsRequired();   
        
        modelBuilder.Entity<PlatformAdmin>()
            .HasOne(gu => gu.GrantedByUser)         
            .WithMany()                     
            .HasForeignKey(gu => gu.GrantedByUserId) 
            .IsRequired(false); 
        
        modelBuilder.Entity<PlatformAdmin>()
            .HasOne(ru => ru.RevokedByUser)         
            .WithMany()                     
            .HasForeignKey(ru => ru.RevokedByUserId) 
            .IsRequired(false); 

    }
}