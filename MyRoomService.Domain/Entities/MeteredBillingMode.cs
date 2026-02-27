using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MyRoomService.Domain.Entities
{
    public enum MeteredBillingMode
    {
        /// <summary>
        /// The total metered charge is split equally among all active contracts in the unit.
        /// </summary>
        [Display(Name = "Split Equally")]
        SplitEqually = 0,

        /// <summary>
        /// The total metered charge is billed only to the primary/first occupant.
        /// </summary>
        [Display(Name = "Single Occupant")]
        SingleOccupant = 1
    }
}
