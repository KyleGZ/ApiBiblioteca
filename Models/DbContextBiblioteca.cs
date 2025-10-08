using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ApiBiblioteca.Models;

public partial class DbContextBiblioteca : DbContext
{
    public DbContextBiblioteca()
    {
    }

    public DbContextBiblioteca(DbContextOptions<DbContextBiblioteca> options)
        : base(options)
    {
    }

    public virtual DbSet<Autor> Autors { get; set; }

    public virtual DbSet<Bitacora> Bitacoras { get; set; }

    public virtual DbSet<Configuracion> Configuracions { get; set; }

    public virtual DbSet<Editorial> Editorials { get; set; }

    public virtual DbSet<Evento> Eventos { get; set; }

    public virtual DbSet<Genero> Generos { get; set; }

    public virtual DbSet<InscripcionEvento> InscripcionEventos { get; set; }

    public virtual DbSet<Libro> Libros { get; set; }

    public virtual DbSet<Notificacion> Notificacions { get; set; }

    public virtual DbSet<Prestamo> Prestamos { get; set; }

    public virtual DbSet<Reserva> Reservas { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<Sancion> Sancions { get; set; }

    public virtual DbSet<Seccion> Seccions { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=THINKBOOK-14-G2\\MSSQLSERVER01;Database=biblioteca_municipal;User Id=Greivin;Password=1234;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Autor>(entity =>
        {
            entity.HasKey(e => e.IdAutor).HasName("PK__autor__5FC3872DA40475FE");

            entity.ToTable("autor");

            entity.Property(e => e.IdAutor).HasColumnName("id_autor");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Bitacora>(entity =>
        {
            entity.HasKey(e => e.IdBitacora).HasName("PK__bitacora__7E4268B05F5B9162");

            entity.ToTable("bitacora");

            entity.Property(e => e.IdBitacora).HasColumnName("id_bitacora");
            entity.Property(e => e.Accion)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("accion");
            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("fecha_hora");
            entity.Property(e => e.IdRegistro).HasColumnName("id_registro");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.TablaAfectada)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("tabla_afectada");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Bitacoras)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__bitacora__id_usu__73BA3083");
        });

        modelBuilder.Entity<Configuracion>(entity =>
        {
            entity.HasKey(e => e.IdConfiguracion).HasName("PK__configur__16A13EBD16350C83");

            entity.ToTable("configuracion");

            entity.HasIndex(e => e.Clave, "UQ__configur__71DCA3DB0EA5A744").IsUnique();

            entity.Property(e => e.IdConfiguracion).HasColumnName("id_configuracion");
            entity.Property(e => e.Clave)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("clave");
            entity.Property(e => e.Descripcion)
                .HasColumnType("text")
                .HasColumnName("descripcion");
            entity.Property(e => e.Valor)
                .HasColumnType("text")
                .HasColumnName("valor");
        });

        modelBuilder.Entity<Editorial>(entity =>
        {
            entity.HasKey(e => e.IdEditorial).HasName("PK__editoria__10C1DD029FC74BAB");

            entity.ToTable("editorial");

            entity.Property(e => e.IdEditorial).HasColumnName("id_editorial");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.IdEvento).HasName("PK__evento__AF150CA580ACA358");

            entity.ToTable("evento");

            entity.Property(e => e.IdEvento).HasColumnName("id_evento");
            entity.Property(e => e.AforoMaximo).HasColumnName("aforo_maximo");
            entity.Property(e => e.Descripcion)
                .HasColumnType("text")
                .HasColumnName("descripcion");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.Titulo)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("titulo");
        });

        modelBuilder.Entity<Genero>(entity =>
        {
            entity.HasKey(e => e.IdGenero).HasName("PK__genero__99A8E4F93BA071A6");

            entity.ToTable("genero");

            entity.Property(e => e.IdGenero).HasColumnName("id_genero");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<InscripcionEvento>(entity =>
        {
            entity.HasKey(e => e.IdInscripcionEvento).HasName("PK__inscripc__D4B74E62984155BE");

            entity.ToTable("inscripcion_evento");

            entity.Property(e => e.IdInscripcionEvento).HasColumnName("id_inscripcion_evento");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("confirmada")
                .HasColumnName("estado");
            entity.Property(e => e.FechaInscripcion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("fecha_inscripcion");
            entity.Property(e => e.IdEvento).HasColumnName("id_evento");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.InscripcionEventos)
                .HasForeignKey(d => d.IdEvento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__inscripci__id_ev__6B24EA82");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.InscripcionEventos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__inscripci__id_us__6C190EBB");
        });

        modelBuilder.Entity<Libro>(entity =>
        {
            entity.HasKey(e => e.IdLibro).HasName("PK__libro__EC09C24EAE163622");

            entity.ToTable("libro");

            entity.HasIndex(e => e.Isbn, "UQ__libro__99F9D0A4076E98FB").IsUnique();

            entity.Property(e => e.IdLibro).HasColumnName("id_libro");
            entity.Property(e => e.Descripcion)
                .HasColumnType("text")
                .HasColumnName("descripcion");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("estado");
            entity.Property(e => e.IdAutor).HasColumnName("id_autor");
            entity.Property(e => e.IdEditorial).HasColumnName("id_editorial");
            entity.Property(e => e.IdGenero).HasColumnName("id_genero");
            entity.Property(e => e.IdSeccion).HasColumnName("id_seccion");
            entity.Property(e => e.Isbn)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("isbn");
            entity.Property(e => e.PortadaUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("portada_url");
            entity.Property(e => e.Titulo)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("titulo");

            entity.HasOne(d => d.IdAutorNavigation).WithMany(p => p.Libros)
                .HasForeignKey(d => d.IdAutor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__libro__id_autor__4CA06362");

            entity.HasOne(d => d.IdEditorialNavigation).WithMany(p => p.Libros)
                .HasForeignKey(d => d.IdEditorial)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__libro__id_editor__4D94879B");

            entity.HasOne(d => d.IdGeneroNavigation).WithMany(p => p.Libros)
                .HasForeignKey(d => d.IdGenero)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__libro__id_genero__4E88ABD4");

            entity.HasOne(d => d.IdSeccionNavigation).WithMany(p => p.Libros)
                .HasForeignKey(d => d.IdSeccion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__libro__id_seccio__4F7CD00D");
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion).HasName("PK__notifica__8270F9A5E96D3290");

            entity.ToTable("notificacion");

            entity.Property(e => e.IdNotificacion).HasColumnName("id_notificacion");
            entity.Property(e => e.Asunto)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("asunto");
            entity.Property(e => e.FechaEnvio)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("fecha_envio");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Mensaje)
                .HasColumnType("text")
                .HasColumnName("mensaje");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Notificacions)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__notificac__id_us__6FE99F9F");
        });

        modelBuilder.Entity<Prestamo>(entity =>
        {
            entity.HasKey(e => e.IdPrestamo).HasName("PK__prestamo__5E87BE27FB5DAC9F");

            entity.ToTable("prestamo");

            entity.Property(e => e.IdPrestamo).HasColumnName("id_prestamo");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("activo")
                .HasColumnName("estado");
            entity.Property(e => e.FechaDevolucionPrevista).HasColumnName("fecha_devolucion_prevista");
            entity.Property(e => e.FechaDevolucionReal).HasColumnName("fecha_devolucion_real");
            entity.Property(e => e.FechaPrestamo)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("fecha_prestamo");
            entity.Property(e => e.IdLibro).HasColumnName("id_libro");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Renovaciones).HasColumnName("renovaciones");

            entity.HasOne(d => d.IdLibroNavigation).WithMany(p => p.Prestamos)
                .HasForeignKey(d => d.IdLibro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__prestamo__id_lib__5629CD9C");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Prestamos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__prestamo__id_usu__571DF1D5");
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(e => e.IdReserva).HasName("PK__reserva__423CBE5D6EA96448");

            entity.ToTable("reserva");

            entity.Property(e => e.IdReserva).HasColumnName("id_reserva");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("activa")
                .HasColumnName("estado");
            entity.Property(e => e.FechaReserva)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("fecha_reserva");
            entity.Property(e => e.IdLibro).HasColumnName("id_libro");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Prioridad)
                .HasDefaultValue(1)
                .HasColumnName("prioridad");

            entity.HasOne(d => d.IdLibroNavigation).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.IdLibro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__reserva__id_libr__5EBF139D");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__reserva__id_usua__5DCAEF64");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("PK__rol__6ABCB5E01526C87D");

            entity.ToTable("rol");

            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Descripcion)
                .HasColumnType("text")
                .HasColumnName("descripcion");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("activo")
                .HasColumnName("estado");
            entity.Property(e => e.NombreRol)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nombre_rol");
        });

        modelBuilder.Entity<Sancion>(entity =>
        {
            entity.HasKey(e => e.IdSancion).HasName("PK__sancion__40D35AF355DBA6A6");

            entity.ToTable("sancion");

            entity.Property(e => e.IdSancion).HasColumnName("id_sancion");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("activa")
                .HasColumnName("estado");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Motivo)
                .HasColumnType("text")
                .HasColumnName("motivo");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Sancions)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__sancion__id_usua__6383C8BA");
        });

        modelBuilder.Entity<Seccion>(entity =>
        {
            entity.HasKey(e => e.IdSeccion).HasName("PK__seccion__7C91FD8104F463F1");

            entity.ToTable("seccion");

            entity.Property(e => e.IdSeccion).HasColumnName("id_seccion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.Ubicacion)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("ubicacion");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK__usuario__4E3E04ADFDDD2E91");

            entity.ToTable("usuario");

            entity.HasIndex(e => e.Email, "UQ__usuario__AB6E616480AA5CFF").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("estado");
            entity.Property(e => e.FechaRegistro).HasColumnName("fecha_registro");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");

            entity.HasMany(d => d.IdRols).WithMany(p => p.IdUsuarios)
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioRol",
                    r => r.HasOne<Rol>().WithMany()
                        .HasForeignKey("IdRol")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__usuario_r__id_ro__403A8C7D"),
                    l => l.HasOne<Usuario>().WithMany()
                        .HasForeignKey("IdUsuario")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__usuario_r__id_us__3F466844"),
                    j =>
                    {
                        j.HasKey("IdUsuario", "IdRol").HasName("PK__usuario___5895CFF3C71D44EF");
                        j.ToTable("usuario_rol");
                        j.IndexerProperty<int>("IdUsuario").HasColumnName("id_usuario");
                        j.IndexerProperty<int>("IdRol").HasColumnName("id_rol");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
