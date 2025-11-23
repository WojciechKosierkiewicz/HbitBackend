using Microsoft.EntityFrameworkCore;
using HbitBackend.Models.User;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace HbitBackend.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.UserName).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}



