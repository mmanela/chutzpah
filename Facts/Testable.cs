using System;
using Moq;
using StructureMap.AutoMocking.Moq;

namespace Chutzpah.Facts
{
    public class Testable<TClassUnderTest> where TClassUnderTest : class
    {
        protected MoqAutoMocker<TClassUnderTest> autoMocker = new MoqAutoMocker<TClassUnderTest>();

        public Testable()
        {

        }

        public Testable(Action<Testable<TClassUnderTest>> setup)
        {
            setup(this);
        }

        public Mock<TDependencyToMock> Mock<TDependencyToMock>() where TDependencyToMock : class
        {
            var a = autoMocker.Get<TDependencyToMock>();
            return Moq.Mock.Get(a);
        }

        public void Inject<T>(T type)
        {
            autoMocker.Inject(typeof(T), type);
        }

        public void InjectArray<T>(T[] types)
        {
            autoMocker.InjectArray(types);
        }

        public void MockSelf()
        {
            autoMocker.PartialMockTheClassUnderTest();
        }

        public TClassUnderTest ClassUnderTest
        {
            get { return autoMocker.ClassUnderTest; }
        }
    }
}
