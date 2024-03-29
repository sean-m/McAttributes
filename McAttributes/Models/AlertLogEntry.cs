﻿using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models
{
    [Table("idalerts")]
    public class AlertLogEntry : RowVersionedModel {

        [Column("attrname")]
        public string? AttrName { get; set; }

        [Column("created")]
        public DateTime? Created { get; set; }

        [Column("lastSeen")]
        public DateTime? LastSeen { get; set; }

        [Column("alerthash")]
        public string? AlertHash { get; set; }
        
        private const string statusErrMsg = "Allowed values: review, resolved, denied. Remove leading & trailing whitespace.";
        [RegularExpression(@"^(review|resolved|denied)$", ErrorMessage = statusErrMsg, MatchTimeoutInMilliseconds = 100)]
        [Column("status")]
        public string? Status { get; set; }

        [DisplayFormat(HtmlEncode = true)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }
    }

    public enum LogLevel
    {
        debug=0,
        info,
        warning,
        error,
        critical
    }
}
