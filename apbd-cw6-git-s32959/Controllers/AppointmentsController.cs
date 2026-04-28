using apbd_cw6_git_s32959.DTOs;
using apbd_cw6_git_s32959.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace apbd_cw6_git_s32959.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentsRepository _repository;

        public AppointmentsController(IAppointmentsRepository repository)
        {
            _repository = repository;
        }
        
        [HttpGet]
        public IActionResult GetAll([FromQuery] GetAppointmentsQuery query)
        {
            try
            {
                List<AppointmentListDto> output = _repository.GetAppointments(query.status, query.patientLastName).Result;
                if (output.Count > 0) return Ok(output);
                return NotFound();
            }
            catch (Exception e)
            {
                ErrorResponseDto errorResponseDto = new ErrorResponseDto();
                errorResponseDto.msg = e.Message;
                return BadRequest(errorResponseDto);
            }
        }

        [HttpGet("{idAppointment:int}")]
        public IActionResult GetAppointmentById(int idAppointment)
        {
            try
            {
                AppointmentDetailsDto? output = _repository.GetAppointmentById(idAppointment).Result;
                if (output != null) return Ok(output);
                return NotFound();    
            }
            catch (Exception e)
            {
                ErrorResponseDto errorResponseDto = new ErrorResponseDto();
                errorResponseDto.msg = e.Message;
                return BadRequest(errorResponseDto);
            }
        }

        [HttpPost]
        public IActionResult CreateAppointment(CreateAppointmentRequestDto dto)
        {
            try
            {
                if (_repository.DoesActivePatientExist(dto.IdPatient).Result == 0)
                {
                    Console.Write("meow");
                    return Conflict();
                }
                if (_repository.DoesActiveDoctorExist(dto.IdPatient).Result == 0) return Conflict();
                if (dto.AppointmentDate < DateTime.Today) return Conflict();
                if (String.IsNullOrEmpty(dto.Reason) || dto.Reason.Length>250) return Conflict();
                if (_repository.GetAmountOfVisitsForDoctorDuringDate(dto.IdDoctor, dto.AppointmentDate).Result != 0) return Conflict();
                int result = _repository.CreateAppoitnmentFromDto(dto).Result;
                if (result == 0) return Conflict();
                return Created();
            }
            catch (Exception e)
            {
                ErrorResponseDto errorResponseDto = new ErrorResponseDto();
                errorResponseDto.msg = e.Message;
                return BadRequest(errorResponseDto);
            }
        }

        [HttpPut("{idAppointment:int}")]
        public IActionResult UpdateAppointment(int idAppointment, UpdateAppointmentRequestDto dto)
        {
            List<String> validStatusList = new List<string>() { "Scheduled", "Completed", "Cancelled" };
            AppointmentDetailsDto? appointment = _repository.GetAppointmentById(idAppointment).Result;
            if (appointment == null) return NotFound();
            if (appointment.Status == "Completed") return Conflict();
            if (!validStatusList.Contains(dto.Status)) return Conflict();
            if (_repository.DoesActiveDoctorExist(dto.IdDoctor).Result == 0) return Conflict();
            if (_repository.DoesActivePatientExist(dto.IdPatient).Result == 0) return Conflict();
            if (!appointment.AppointmentDate.Equals(dto.AppointmentDate) && _repository
                    .GetAmountOfVisitsForDoctorDuringDate(dto.IdDoctor, dto.AppointmentDate).Result != 0) 
                return Conflict();

            _repository.UpdateAppointmentFromDto(dto, idAppointment);
            AppointmentDetailsDto result = _repository.GetAppointmentById(idAppointment).Result; 
            return Ok(result);
        }

        [HttpDelete("{idAppointment:int}")]
        public IActionResult DeleteAppointment(int idAppointment) {
            AppointmentDetailsDto? appointment = _repository.GetAppointmentById(idAppointment).Result;
            if (appointment==null) return NotFound();
            if (appointment.Status.Equals("Completed")) return Conflict();
            int amountDeleted = _repository.DeleteAppointment(idAppointment).Result;
            if (amountDeleted == 0) return NotFound();
            return NoContent();
        }
    }

    public class GetAppointmentsQuery
    {
        [FromQuery] public string status { get; set; } = string.Empty;
        [FromQuery] public string patientLastName { get; set; } = string.Empty;
    }
}
