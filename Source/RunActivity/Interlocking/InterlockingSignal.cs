﻿/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
///
/// Principal Author:
///     Author: Charlie Salts / Signalsoft Rail Consultancy Ltd.
/// Contributor:
///    Richard Plokhaar / Signalsoft Rail Consultancy Ltd.
/// 


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS.Interlocking
{
   /// <summary>
   /// Provides an abstraction atop an underlying signal
   /// object.
   /// </summary>
   public class InterlockingSignal : InterlockingTerminator
   {
     
      /// <summary>
      /// Gets the underlying SignalObject.
      /// </summary>
      public SignalObject SignalObject { get; private set; }


      /// <summary>
      /// Creates a new InterlockingSignal object.
      /// </summary>
      /// <param name="simulator"></param>
      /// <param name="signalObject"></param>
      public InterlockingSignal(Simulator simulator, SignalObject signalObject) : base(simulator)
      {
         
         this.SignalObject = signalObject;
      }





      /// <summary>
      /// Returns true when any of the signal heads show any "proceed" aspect.
      /// </summary>
      public bool IsShowingAnyProceed
      {
         get
         {
            bool returnValue = false;

            foreach (var signalHead in SignalObject.SignalHeads)
            {
               if (signalHead.state == SignalHead.SIGASP.CLEAR_1 ||
                   signalHead.state == SignalHead.SIGASP.CLEAR_2 )
               {
                  returnValue = true;

                  // we have found *one* signal head showing *some* clear aspect - we're done
                  break;
               }
                
            }

            return returnValue;
         }
      }
   }
}
