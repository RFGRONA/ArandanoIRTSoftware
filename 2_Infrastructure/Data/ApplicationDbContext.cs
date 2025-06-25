using System;
using System.Collections.Generic;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._0_Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ArandanoIRT.Web._2_Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Crop> Crops { get; set; }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<DeviceActivation> DeviceActivations { get; set; }

    public virtual DbSet<DeviceToken> DeviceTokens { get; set; }

    public virtual DbSet<EnvironmentalReading> EnvironmentalReadings { get; set; }

    public virtual DbSet<InvitationCode> InvitationCodes { get; set; }

    public virtual DbSet<Observation> Observations { get; set; }

    public virtual DbSet<Plant> Plants { get; set; }

    public virtual DbSet<ThermalCapture> ThermalCaptures { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum<ActivationStatus>()
            .HasPostgresEnum<DeviceStatus>()
            .HasPostgresEnum<TokenStatus>();

        modelBuilder.Entity<Crop>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("crops_pkey");

            entity.ToTable("crops", tb => tb.HasComment("Stores information about crops. Acts as the main grouping entity (tenant)."));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.AdminUserId).HasColumnName("admin_user_id");
            entity.Property(e => e.CityName).HasColumnName("city_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.AdminUser).WithMany(p => p.Crops)
                .HasForeignKey(d => d.AdminUserId)
                .HasConstraintName("fk_crops_admin_user");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("devices_pkey");

            entity.ToTable("devices", tb => tb.HasComment("Stores physical monitoring hardware devices."));

            entity.HasIndex(e => e.MacAddress, "devices_mac_address_key").IsUnique();

            entity.HasIndex(e => e.MacAddress, "idx_devices_mac_address");

            entity.HasIndex(e => e.PlantId, "idx_devices_plant_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CropId).HasColumnName("crop_id");
            entity.Property(e => e.DataCollectionIntervalMinutes)
                .HasDefaultValue((short)15)
                .HasColumnName("data_collection_interval_minutes");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.MacAddress).HasColumnName("mac_address");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.PlantId).HasColumnName("plant_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.RegisteredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("registered_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Crop).WithMany(p => p.Devices)
                .HasForeignKey(d => d.CropId)
                .HasConstraintName("devices_crop_id_fkey");

            entity.HasOne(d => d.Plant).WithMany(p => p.Devices)
                .HasForeignKey(d => d.PlantId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("devices_plant_id_fkey");
        });

        modelBuilder.Entity<DeviceActivation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("device_activations_pkey");

            entity.ToTable("device_activations", tb => tb.HasComment("Stores single-use codes to activate new devices."));

            entity.HasIndex(e => e.ActivationCode, "device_activations_activation_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActivatedAt).HasColumnName("activated_at");
            entity.Property(e => e.ActivationCode).HasColumnName("activation_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceActivations)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("device_activations_device_id_fkey");
        });

        modelBuilder.Entity<DeviceToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("device_tokens_pkey");

            entity.ToTable("device_tokens", tb => tb.HasComment("Stores authentication tokens (JWTs) for devices."));

            entity.HasIndex(e => e.AccessToken, "device_tokens_access_token_key").IsUnique();

            entity.HasIndex(e => e.RefreshToken, "device_tokens_refresh_token_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccessToken).HasColumnName("access_token");
            entity.Property(e => e.AccessTokenExpiresAt).HasColumnName("access_token_expires_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExpiresAt).HasColumnName("refresh_token_expires_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceTokens)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("device_tokens_device_id_fkey");
        });

        modelBuilder.Entity<EnvironmentalReading>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("environmental_readings_pkey");

            entity.ToTable("environmental_readings", tb => tb.HasComment("Stores environmental data collected by device sensors."));

            entity.HasIndex(e => new { e.DeviceId, e.RecordedAtServer }, "idx_readings_device_id_recorded_at_server").IsDescending(false, true);

            entity.HasIndex(e => new { e.PlantId, e.RecordedAtServer }, "idx_readings_plant_id_recorded_at_server").IsDescending(false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CityHumidity).HasColumnName("city_humidity");
            entity.Property(e => e.CityTemperature).HasColumnName("city_temperature");
            entity.Property(e => e.CityWeatherCondition).HasColumnName("city_weather_condition");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.ExtraData)
                .HasColumnType("jsonb")
                .HasColumnName("extra_data");
            entity.Property(e => e.Humidity).HasColumnName("humidity");
            entity.Property(e => e.PlantId).HasColumnName("plant_id");
            entity.Property(e => e.RecordedAtDevice)
                .HasDefaultValueSql("now()")
                .HasColumnName("recorded_at_device");
            entity.Property(e => e.RecordedAtServer)
                .HasDefaultValueSql("now()")
                .HasColumnName("recorded_at_server");
            entity.Property(e => e.Temperature).HasColumnName("temperature");

            entity.HasOne(d => d.Device).WithMany(p => p.EnvironmentalReadings)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("environmental_readings_device_id_fkey");

            entity.HasOne(d => d.Plant).WithMany(p => p.EnvironmentalReadings)
                .HasForeignKey(d => d.PlantId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("environmental_readings_plant_id_fkey");
        });

        modelBuilder.Entity<InvitationCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invitation_codes_pkey");

            entity.ToTable("invitation_codes", tb => tb.HasComment("Stores single-use invitation codes for user registration."));

            entity.HasIndex(e => e.CreatedByUserId, "idx_invitation_codes_created_by_user_id");

            entity.HasIndex(e => e.Code, "invitation_codes_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(255)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.InvitationCodes)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("invitation_codes_created_by_user_id_fkey");
        });

        modelBuilder.Entity<Observation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("observations_pkey");

            entity.ToTable("observations", tb => tb.HasComment("Stores manual observations made by an agronomist or expert user."));

            entity.HasIndex(e => e.PlantId, "idx_observations_plant_id");

            entity.HasIndex(e => e.UserId, "idx_observations_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PlantId).HasColumnName("plant_id");
            entity.Property(e => e.SubjectiveRating).HasColumnName("subjective_rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Plant).WithMany(p => p.Observations)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("observations_plant_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Observations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("observations_user_id_fkey");
        });

        modelBuilder.Entity<Plant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("plants_pkey");

            entity.ToTable("plants", tb => tb.HasComment("Stores data for each monitored plant."));

            entity.HasIndex(e => e.CropId, "idx_plants_crop_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CropId).HasColumnName("crop_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.RegisteredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("registered_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Crop).WithMany(p => p.Plants)
                .HasForeignKey(d => d.CropId)
                .HasConstraintName("plants_crop_id_fkey");
        });

        modelBuilder.Entity<ThermalCapture>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("thermal_captures_pkey");

            entity.ToTable("thermal_captures", tb => tb.HasComment("Stores thermographic captures. Statistics are stored in JSONB, the image path in Object Storage."));

            entity.HasIndex(e => new { e.DeviceId, e.RecordedAtServer }, "idx_captures_device_id_recorded_at_server").IsDescending(false, true);

            entity.HasIndex(e => new { e.PlantId, e.RecordedAtServer }, "idx_captures_plant_id_recorded_at_server").IsDescending(false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.PlantId).HasColumnName("plant_id");
            entity.Property(e => e.RecordedAtDevice)
                .HasDefaultValueSql("now()")
                .HasColumnName("recorded_at_device");
            entity.Property(e => e.RecordedAtServer)
                .HasDefaultValueSql("now()")
                .HasColumnName("recorded_at_server");
            entity.Property(e => e.RgbImagePath).HasColumnName("rgb_image_path");
            entity.Property(e => e.ThermalDataStats)
                .HasColumnType("jsonb")
                .HasColumnName("thermal_data_stats");

            entity.HasOne(d => d.Device).WithMany(p => p.ThermalCaptures)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("thermal_captures_device_id_fkey");

            entity.HasOne(d => d.Plant).WithMany(p => p.ThermalCaptures)
                .HasForeignKey(d => d.PlantId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("thermal_captures_plant_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users", tb => tb.HasComment("Stores web application users and their credentials."));

            entity.HasIndex(e => e.CropId, "idx_users_crop_id");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CropId).HasColumnName("crop_id");
            entity.Property(e => e.Email)
                .HasMaxLength(75)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(40)
                .HasColumnName("first_name");
            entity.Property(e => e.IsAdmin)
                .HasDefaultValue(false)
                .HasColumnName("is_admin");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.LastName)
                .HasMaxLength(40)
                .HasColumnName("last_name");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Crop).WithMany(p => p.Users)
                .HasForeignKey(d => d.CropId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("users_crop_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
