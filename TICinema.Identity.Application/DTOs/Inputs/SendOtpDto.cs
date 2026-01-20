using System;
using System.Collections.Generic;
using System.Text;

namespace TICinema.Identity.Application.DTOs.Inputs
{
    public record SendOtpDto(string Identifier, string Type);
}
