using Microsoft.EntityFrameworkCore;
using SGCRN.Models;

namespace SGCRN.Data;

public sealed class SGCRNDbContext(DbContextOptions<SGCRNDbContext> options) : DbContext(options)
{
    public DbSet<ProductoTerminado> ProductosTerminados => Set<ProductoTerminado>();
    public DbSet<MateriaPrima> MateriasPrimas => Set<MateriaPrima>();
    public DbSet<EntradaInventario> EntradasInventario => Set<EntradaInventario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var productos = modelBuilder.Entity<ProductoTerminado>();
        productos.ToTable("ProductosTerminados");
        productos.HasKey(p => p.Id);
        productos.Property(p => p.Id).ValueGeneratedOnAdd();
        productos.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
        productos.HasIndex(p => p.Nombre).IsUnique();
        productos.Property(p => p.Descripcion).HasMaxLength(500);
        productos.Property(p => p.ImagenUrl).HasMaxLength(500);
        productos.Property(p => p.UnidadMedida).HasMaxLength(30).IsRequired();
        productos.Property(p => p.PrecioSugerido).HasColumnType("decimal(18,2)");
        productos.Property(p => p.StockActual).HasColumnType("decimal(18,3)");
        productos.Property(p => p.Activo).HasDefaultValue(true);
        productos.Property(p => p.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
        productos.Property(p => p.ActualizadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAddOrUpdate();

        var materias = modelBuilder.Entity<MateriaPrima>();
        materias.ToTable("MateriasPrimas");
        materias.HasKey(m => m.Id);
        materias.Property(m => m.Id).ValueGeneratedOnAdd();
        materias.Property(m => m.Nombre).HasMaxLength(150).IsRequired();
        materias.HasIndex(m => m.Nombre).IsUnique();
        materias.Property(m => m.Descripcion).HasMaxLength(500);
        materias.Property(m => m.UnidadMedida).HasMaxLength(30).IsRequired();
        materias.Property(m => m.StockActual).HasColumnType("decimal(18,3)");
        materias.Property(m => m.StockMinimo).HasColumnType("decimal(18,3)");
        materias.Property(m => m.Activo).HasDefaultValue(true);
        materias.Property(m => m.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
        materias.Property(m => m.ActualizadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAddOrUpdate();

        var entradas = modelBuilder.Entity<EntradaInventario>();
        entradas.ToTable("EntradasInventario");
        entradas.HasKey(e => e.Id);
        entradas.Property(e => e.Id).ValueGeneratedOnAdd();
        entradas.Property(e => e.Fecha).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
        entradas.Property(e => e.TipoOrigen).HasConversion<string>().HasMaxLength(20).IsRequired();
        entradas.Property(e => e.Cantidad).HasColumnType("decimal(18,3)");
        entradas.Property(e => e.CostoUnitario).HasColumnType("decimal(18,2)");
        entradas.Property(e => e.Observaciones).HasMaxLength(500);
        entradas.HasOne(e => e.MateriaPrima).WithMany().HasForeignKey(e => e.MateriaPrimaId)
            .OnDelete(DeleteBehavior.Restrict);
        entradas.HasOne(e => e.ProductoTerminado).WithMany().HasForeignKey(e => e.ProductoTerminadoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
