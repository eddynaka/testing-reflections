using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"reflection: {MeasureExecTime(ActivityTester.ActivityWithReflectionVoid)}");
            Console.WriteLine($"expression: {MeasureExecTime(ActivityTester.ActivityWithExpressionVoid)}");

            var summary = BenchmarkRunner.Run<ActivityTester>();
            //var summary = BenchmarkRunner.Run<ActivityNameTester>();
            Console.WriteLine(summary);
            Console.ReadKey();
        }

        private static long MeasureExecTime(Action action)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            action();
            watch.Stop();
            return watch.ElapsedMilliseconds;
        }

    }

    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.NetCoreApp21)]
    [SimpleJob(RuntimeMoniker.Net461)]
    [MemoryDiagnoser]
    public class ActivityTester
    {
        static Action<Activity, ActivityKind> setterNameProperty = CreateSetter("Kind");

        [Benchmark]
        public Activity ActivityWithReflection()
        {
            Activity activity = new Activity("activity-with-reflection");
            activity.GetType().GetProperty("Kind").SetValue(activity, ActivityKind.Client);

            return activity;
        }

        public static void ActivityWithReflectionVoid()
        {
            for (int i = 0; i < 1_000_000; i++)
            {
                Activity activity = new Activity("activity-with-reflection");
                activity.GetType().GetProperty("Kind").SetValue(activity, ActivityKind.Client);
            }
        }

        [Benchmark]
        public Activity ActivityWithExpression()
        {
            var activity = new Activity("activity-with-expression");

            setterNameProperty(activity, ActivityKind.Client);

            return activity;
        }

        public static void ActivityWithExpressionVoid()
        {
            for (int i = 0; i < 1_000_000; i++)
            {
                var activity = new Activity("activity-with-expression");

                setterNameProperty(activity, ActivityKind.Client);
            }
        }

        public static Action<Activity, ActivityKind> CreateSetter(string name)
        {
            ParameterExpression instance = Expression.Parameter(typeof(Activity), "instance");
            ParameterExpression propertyValue = Expression.Parameter(typeof(ActivityKind), "propertyValue");

            var body = Expression.Assign(Expression.Property(instance, name), propertyValue);

            return Expression.Lambda<Action<Activity, ActivityKind>>(body, instance, propertyValue).Compile();
        }
    }


    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.NetCoreApp21)]
    [SimpleJob(RuntimeMoniker.Net461)]
    [MemoryDiagnoser]
    public class ActivityNameTester
    {
        static Action<Activity, string> setterNameProperty = CreateSetter("DisplayName");
        static MyProp prop;

        public ActivityNameTester()
        {
            prop = CreatePropertyDictionary(typeof(Activity));
        }

        [Benchmark]
        public Activity ActivityPure()
        {
            Activity activity = new Activity("activity-with-reflection");
            activity.DisplayName = "new-name";

            return activity;
        }

        [Benchmark]
        public Activity ActivityWithReflection()
        {
            Activity activity = new Activity("activity-with-reflection");
            activity.GetType().GetProperty("DisplayName").SetValue(activity, "new-name");

            return activity;
        }

        [Benchmark]
        public Activity ActivityWithExpression()
        {
            var activity = new Activity("activity-with-expression");

            setterNameProperty(activity, "new-name");

            return activity;
        }

        [Benchmark]
        public Activity ActivityWithDynamicMethod()
        {
            var activity = new Activity("activity-with-dynamic-method");
            prop.Setter(activity, "new-name");

            return activity;
        }

        public static Action<Activity, string> CreateSetter(string name)
        {
            ParameterExpression instance = Expression.Parameter(typeof(Activity), "instance");
            ParameterExpression propertyValue = Expression.Parameter(typeof(string), "propertyValue");

            var body = Expression.Assign(Expression.Property(instance, name), propertyValue);

            return Expression.Lambda<Action<Activity, string>>(body, instance, propertyValue).Compile();
        }

        private static MyProp CreatePropertyDictionary(Type type)
        {
            var prop = type.GetProperty("DisplayName");
            //var props = new MyProp[allProps.Length];

            //for (int i = 0; i < allProps.Length; i++)
            //{
            //    var prop = allProps[i];
            // Getter dynamic method the signature would be :
            // object Get(object thisReference)
            // { return ((TestClass)thisReference).Prop; }

            //DynamicMethod dmGet = new DynamicMethod("Get", typeof(object),
            //                                     new Type[] { typeof(object), });
            //ILGenerator ilGet = dmGet.GetILGenerator();
            //// Load first argument to the stack
            //ilGet.Emit(OpCodes.Ldarg_0);
            //// Cast the object on the stack to the apropriate type
            //ilGet.Emit(OpCodes.Castclass, type);
            //// Call the getter method passing the object on teh stack as the this reference
            //ilGet.Emit(OpCodes.Callvirt, prop.GetGetMethod());
            //// If the property type is a value type (int/DateTime/..)
            //// box the value so we can return it
            //if (prop.PropertyType.IsValueType)
            //{
            //    ilGet.Emit(OpCodes.Box, prop.PropertyType);
            //}
            //// Return from the method
            //ilGet.Emit(OpCodes.Ret);


            // Getter dynamic method the signature would be :
            // object Set(object thisReference, object propValue)
            // { return ((TestClass)thisReference).Prop = (PropType)propValue; }

            DynamicMethod dmSet = new DynamicMethod("Set", typeof(void),
                                         new Type[] { typeof(object), typeof(object) });
            ILGenerator ilSet = dmSet.GetILGenerator();
            // Load first argument to the stack and cast it
            ilSet.Emit(OpCodes.Ldarg_0);
            ilSet.Emit(OpCodes.Castclass, type);

            // Load secons argument to the stack and cast it or unbox it
            ilSet.Emit(OpCodes.Ldarg_1);
            if (prop.PropertyType.IsValueType)
            {
                ilSet.Emit(OpCodes.Unbox_Any, prop.PropertyType);
            }
            else
            {
                ilSet.Emit(OpCodes.Castclass, prop.PropertyType);
            }
            // Call Setter method and return
            ilSet.Emit(OpCodes.Callvirt, prop.GetSetMethod());
            ilSet.Emit(OpCodes.Ret);

            // Create the delegates for invoking the dynamic methods and add the to an array for later use
            return new MyProp()
            {
                PropName = prop.Name,
                Setter = (Action<object, object>)
                                 dmSet.CreateDelegate(typeof(Action<object, object>)),
                //Getter = (Func<object, object>)
                //                 dmGet.CreateDelegate(typeof(Func<object, object>)),
            };

            //}
            //return props;
        }
    }

    //Container for getters and setters of a property
    public class MyProp
    {
        public string PropName { get; set; }
        public Func<object, object> Getter { get; set; }
        public Action<object, object> Setter { get; set; }
    }
}
