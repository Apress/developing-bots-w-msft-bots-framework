namespace DoctorAppointmentBot
{
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Connector;
    using System;

    public enum Specialty { Dentist, GeneralPhysician, Psychiatist, Cardiologist, PhysioTherapist }

    // Appointment is the simple form you will fill out to fix an appointment with Doctor. 
    // It must be serializable so that the bot can be stateless.The order of fields defines the default order in which questions will be asked.
    // Enumerations shows the legal options for each field in the SandwichOrder and the order is the order values will be presented 
    // in a conversation.
    [Serializable]
    public class Appointment
    {
        [Prompt("When would you like to book your {&}?")]
        public DateTime AppointmentDate { get; set; }

        [Prompt("What is the {&}")]
        public string PatientName { get; set; }

        [Prompt("What are the {&} you are looking for? {||}")]
        public Specialty? Specialties;

        [Prompt("Any {&} to the Doctor?")]
        public string SpecialInstructions { get; set; }

        public static IForm<Appointment> BuildForm()
        {
            OnCompletionAsyncDelegate<Appointment> processAppointment = async (context, state) =>
            {
                IMessageActivity reply = context.MakeMessage();
                reply.Text = $"We are confirming your appointment for {state.PatientName} at {state.AppointmentDate.ToShortDateString()}, please be on time. " +
                             " Reference ID: " + Guid.NewGuid().ToString().Substring(0, 5);

                // Save State to database here...

                await context.PostAsync(reply);
            };

            return new FormBuilder<Appointment>()
                    .Message("Welcome, I'm Dr.Bot ! I can help with fix an appointment with Doctor.")
                    .OnCompletion(processAppointment)
                    .Build();
        }
    };
}