using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HbitBackend.Models.Friend;

namespace HbitBackend.Data.Configurations;

public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.HasOne(fr => fr.FromUser)
               .WithMany()
               .HasForeignKey(fr => fr.FromUserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fr => fr.ToUser)
               .WithMany()
               .HasForeignKey(fr => fr.ToUserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

