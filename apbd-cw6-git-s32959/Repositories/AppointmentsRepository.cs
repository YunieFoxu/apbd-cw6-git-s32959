using apbd_cw6_git_s32959.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;

namespace apbd_cw6_git_s32959.Repositories;

using Microsoft.Data.SqlClient;

public interface IAppointmentsRepository
{
    Task<List<AppointmentListDto>> GetAppointments(string status, string patientLastName);
    Task<AppointmentDetailsDto?> GetAppointmentById(int id);
    Task<int> DoesActivePatientExist(int id);
    Task<int> DoesActiveDoctorExist(int id);
    Task<int> GetAmountOfVisitsForDoctorDuringDate(int id, DateTime dateTime);
    Task<int> CreateAppoitnmentFromDto(CreateAppointmentRequestDto dto);

    Task<int> UpdateAppointmentFromDto(UpdateAppointmentRequestDto dto, int id);
    Task<int> DeleteAppointment(int id);
}

public class AppointmentsRepository : IAppointmentsRepository{
    private readonly string _connectionString;

    public AppointmentsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException(
                                "Brak ConnectionString 'DefaultConnection' w konfiguracji!");
    }

    public async Task<List<AppointmentListDto>> GetAppointments(string status, string patientLastName){
        List<AppointmentListDto> list = new List<AppointmentListDto>();
        try
        {
            AppointmentListDto dto;
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            string query =
                "SELECT dbo.Appointments.IdAppointment, dbo.Appointments.AppointmentDate, dbo.Appointments.Status, dbo.Appointments.Reason, dbo.Patients.FirstName + ' ' + dbo.Patients.LastName AS FullName, dbo.Patients.Email " +
                "FROM dbo.Appointments " +
                "JOIN dbo.Doctors ON dbo.Appointments.IdDoctor=dbo.Doctors.IdDoctor " +
                "JOIN dbo.Patients ON dbo.Appointments.IdPatient=dbo.Patients.IdPatient " +
                "WHERE (@Status IS NULL OR dbo.Appointments.Status = @Status) " +
                "AND (@PatientLastNanme IS NULL OR dbo.Patients.Lastname = @PatientLastNanme)";

            using var command = new SqlCommand(query, connection);
            
            if (String.IsNullOrEmpty(status)) 
                command.Parameters.AddWithValue("@Status", DBNull.Value);
            else 
                command.Parameters.AddWithValue("@Status", status);
            if (String.IsNullOrEmpty(patientLastName))
                command.Parameters.AddWithValue("@PatientLastNanme", DBNull.Value);
            else
                command.Parameters.AddWithValue("@PatientLastNanme", patientLastName);
            
            using var reader =  await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                dto = new AppointmentListDto();
                dto.IdAppointment = reader.GetInt32(0);
                dto.AppointmentDate = reader.GetDateTime(1);
                dto.Status = reader.GetString(2);
                dto.Reason = reader.GetString(3);
                dto.PatientFullName = reader.GetString(4);
                dto.PatientEmail = reader.GetString(5);
                list.Add(dto);
            }

        }
        catch (SqlException ex)
        {
            // SqlException — błąd specyficzny dla SQL Server
            Console.WriteLine($"Błąd połączenia z bazą: {ex.Message}");
        }

        return list;
    }

    public async Task<AppointmentDetailsDto?> GetAppointmentById(int id){  
        AppointmentDetailsDto? dto = null;
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        string query =
            "SELECT dbo.Appointments.IdAppointment, dbo.Patients.FirstName + ' ' + dbo.Patients.LastName AS PatientFullName, dbo.Patients.Email, dbo.Patients.Email, dbo.Doctors.FirstName + ' ' + dbo.Doctors.LastName, dbo.Doctors.LicenseNumber, dbo.Appointments.AppointmentDate , dbo.Appointments.Status, dbo.Appointments.Reason, dbo.Appointments.InternalNotes, dbo.Appointments.CreatedAt " +
            "FROM dbo.Appointments\nJOIN dbo.Doctors ON dbo.Appointments.IdDoctor=dbo.Doctors.IdDoctor " +
            "JOIN dbo.Patients ON dbo.Appointments.IdPatient=dbo.Patients.IdPatient " +
            "WHERE dbo.Appointments.IdAppointment=@Id";
        
        using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Id", id);
        
        using var reader =  await command.ExecuteReaderAsync();
            
        while (await reader.ReadAsync())
        {
            dto = new AppointmentDetailsDto();
            dto.IdAppointment = reader.GetInt32(0);
            dto.PatientFullName = reader.GetString(1);
            dto.PatientEmail = reader.GetString(2);
            dto.PatientPhoneNumber = reader.GetString(3);
            dto.DoctorFullName = reader.GetString(4);
            dto.DoctorLicenseNumber = reader.GetString(5);
            dto.AppointmentDate = reader.GetDateTime(6);
            dto.Status = reader.GetString(7);
            dto.Reason = reader.GetString(8);
            var internalNotes = reader["InternalNotes"];
            if (internalNotes == DBNull.Value) dto.InternalNotes = String.Empty;
            else dto.InternalNotes = (string)internalNotes;
            dto.CreatedAt = reader.GetDateTime(10);
        }

        return dto;
    }

    public async Task<int> DoesActivePatientExist(int id){
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        string query =
            "SELECT Count(*) "+
            "FROM dbo.Patients " +
            "WHERE dbo.Patients.IsActive='true' AND dbo.Patients.IdPatient=@id";
        
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);

        object? amount = await command.ExecuteScalarAsync();
        return Convert.ToInt32(amount);
    } 
    public async Task<int> DoesActiveDoctorExist(int id){
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        string query =
            "SELECT Count(*) "+
            "FROM dbo.Doctors " +
            "WHERE dbo.Doctors.IsActive='true' AND dbo.Doctors.IdDoctor=@id";
        
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);

        object? amount = await command.ExecuteScalarAsync();
        return Convert.ToInt32(amount);
    }

    public async Task<int> GetAmountOfVisitsForDoctorDuringDate(int id, DateTime dateTime){
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        string query =
            "SELECT Count(*) "+
            "FROM dbo.Doctors " +
            "JOIN dbo.Appointments ON dbo.Doctors.IdDoctor=dbo.Appointments.IdDoctor " +
            "WHERE dbo.Appointments.AppointmentDate=@datetime AND dbo.Doctors.IdDoctor=@id";
        
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@datetime", dateTime);

        object? amount = await command.ExecuteScalarAsync();
        return Convert.ToInt32(amount);
    }

    public async Task<int> CreateAppoitnmentFromDto(CreateAppointmentRequestDto dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        string query =
            "INSERT INTO dbo.Appointments VALUES (@IdPatient, @IdDoctor, @AppointmentDate, 'Scheduled', @Reason, null, GETDATE())";
        
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
        command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
        command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
        command.Parameters.AddWithValue("@Reason", dto.Reason);

        return command.ExecuteNonQueryAsync().Result;
    }

    public async Task<int> UpdateAppointmentFromDto(UpdateAppointmentRequestDto dto, int id)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        string query =
            "UPDATE dbo.Appointments " +
            "SET IdPatient = @IdPatient, " +
	        "IdDoctor = @IdDoctor, " +
	        "AppointmentDate = @AppointmentDate, " +
	        "Status = @Status, " +
	        "Reason = @Reason, " +
	        "InternalNotes = @InternalNotes" +
            "WHERE dbo.Appointments.IdAppointment = @IdAppointment";
        
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdAppointment", id);
        command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
        command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
        command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
        command.Parameters.AddWithValue("@Status", dto.Status);
        command.Parameters.AddWithValue("@Reason", dto.Reason);
        command.Parameters.AddWithValue("@InternalNotes", dto.InternalNotes);

        return command.ExecuteNonQueryAsync().Result;
    }

    public async Task<int> DeleteAppointment(int id){
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        string query = "DELETE FROM dbo.Appointments WHERE dbo.Appointments.IdAppointment=@Id";
        
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);
        
        return command.ExecuteNonQueryAsync().Result;
    }
}