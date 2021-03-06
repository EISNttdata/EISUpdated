﻿namespace EaiConverter.Parser
{
    using EaiConverter.Model;

    public class ActivityParserFactory : IActivityParserFactory
    {
		private readonly XsdParser xsdParser;

		public ActivityParserFactory()
		{
			this.xsdParser = new XsdParser();
		}

        public IActivityParser GetParser(string activityType)
        {
            if (activityType == ActivityType.jdbcQueryActivityType.ToString()
                || activityType == ActivityType.jdbcUpdateActivityType.ToString()
                || activityType == ActivityType.jdbcCallActivityType.ToString())
            {
                return new JdbcQueryActivityParser();
            }
            else if (activityType == ActivityType.callProcessActivityType.ToString())
            {
                return new CallProcessActivityParser();
            }
            else if (activityType == ActivityType.xmlParseActivityType.ToString())
            {
                return new XmlParseActivityParser(this.xsdParser);
            }
            else if (activityType == ActivityType.assignActivityType.ToString())
            {
                return new AssignActivityParser();
            }
            else if (activityType == ActivityType.mapperActivityType.ToString())
            {
                return new MapperActivityParser(this.xsdParser);
            }
            else if (activityType == ActivityType.writeToLogActivityType.ToString())
            {
                return new WriteToLogActivityParser();
            }
            else if (activityType == ActivityType.generateErrorActivity.ToString())
            {
                return new GenerateErrorActivityParser();
            }
            else if (activityType == ActivityType.nullActivityType.ToString() || activityType == ActivityType.OnStartupEventSource.ToString())
            {
                return new NullActivityParser();
            }
            else if (activityType == ActivityType.javaActivityType.ToString())
            {
                return new JavaActivityParser();
            }
            else if (activityType == ActivityType.setSharedVariableActivityType.ToString()
                     || activityType == ActivityType.getSharedVariableActivityType.ToString())
            {
                return new SharedVariableActivityParser();
            }
            else if (activityType == ActivityType.rdvPubActivityType.ToString())
            {
                return new RdvPublishActivityParser();
            }
            else if (activityType == ActivityType.RdvEventSourceActivityType.ToString())
            {
                return new RdvEventSourceActivityParser();
            }
            else if (activityType == ActivityType.sleepActivity.ToString())
            {
                return new SleepActivityParser();
            }
            else if (activityType == ActivityType.TimerEventSource.ToString())
            {
                return new TimerEventActivityParser();
            }
            else if (activityType == ActivityType.AeSubscriberActivity.ToString())
            {
                return new AdapterSubscriberActivityParser();
            }
            else if (activityType == ActivityType.ConfirmActivityType.ToString())
            {
                return new ConfirmActivityParser();
            }
            else if (activityType == ActivityType.EngineCommandActivityType.ToString())
            {
                return new EngineCommandActivityParser();
            }
            else
            {
                return null;
            }
        }
    }
}

