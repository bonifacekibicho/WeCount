﻿using PITCSurveyEntities.Entities;
using PITCSurveyLib.Models;
using PITCSurveySvc.Models;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Data;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Description;

namespace PITCSurveySvc.Controllers
{
	/// <summary>
	/// Controller for Survey Responses (filled-in forms).
	/// </summary>
	public class SurveyResponsesController : BaseController
    {

		// POST: api/SurveyResponses
		/// <summary>
		/// Submit a completed Survey Response.
		/// </summary>
		/// <param name="SurveyResponse"></param>
		/// <param name="DeviceId"></param>
		/// <returns></returns>
		[ResponseType(typeof(void))]
		[SwaggerOperation("PostSurveyResponse")]
		[SwaggerResponse(HttpStatusCode.BadRequest, "The survey data wasn't acceptable (improper formatting, etc.).")]
		[SwaggerResponse(HttpStatusCode.NoContent, "SurveyResponse uploaded successfully.")]
		[AllowAnonymous]
		public IHttpActionResult PostSurveyResponse(SurveyResponseModel SurveyResponse, Guid DeviceId)
		{
			Volunteer sv = GetAuthenticatedVolunteer(DeviceId);

			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			if (sv == null)
				return BadRequest("The specified InterviewerID is not recognized. User not logged in?");

			SurveyResponse sr = db.SurveyResponses.Where(r => r.ResponseIdentifier == SurveyResponse.ResponseIdentifier).SingleOrDefault();
			if (sr != null)
			{
				//return BadRequest("Survey already uploaded.");
				//return StatusCode(HttpStatusCode.Conflict);

				// Delete current response to replace with new one
				db.SurveyResponses.Remove(sr);
			}

			try
			{
				ModelConverter Converter = new ModelConverter(db);

				SurveyResponse Response = Converter.ConvertToEntity(SurveyResponse);

				//Response.Volunteer_ID = 1;
				Response.Volunteer = sv;

				Response.DeviceId = DeviceId;

				Response.DateUploaded = DateTime.UtcNow;

				db.SurveyResponses.Add(Response);

				db.SaveChanges();

				return StatusCode(HttpStatusCode.NoContent);
			}
			catch (DbEntityValidationException evex)
			{
				StringBuilder sb = new StringBuilder();

				foreach (var eve in evex.EntityValidationErrors)
				{
					sb.AppendLine($"{eve.Entry.Entity.GetType().Name}");

					foreach (var ve in eve.ValidationErrors)
					{
						sb.AppendLine($"{ve.PropertyName}: {ve.ErrorMessage}");
					}
				}
				throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, sb.ToString())); // InternalServerError(new ApplicationException(sb.ToString(), evex));
			}
			catch (Exception ex)
			{
				throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.ToString()));	// return Content(HttpStatusCode.InternalServerError, ex.ToString()); // InternalServerError(ex);
			}
		}
	}
}