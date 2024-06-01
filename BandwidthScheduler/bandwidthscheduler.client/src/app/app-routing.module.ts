import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { StaffBuilderComponent } from './staff-builder/staff-builder.component';
import { ScheduleHistoryComponent } from './schedule-history/schedule-history.component';
import { authorizeGuard } from './guards/authorize.guard';
import { authenticateGuard } from './guards/authenticate.guard';
import { ScheduleComponent } from './schedule/schedule.component';
import { AvailabilityAndCommitmentsComponent } from './availability-and-commitments/availability-and-commitments.component';

const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: '/home',
  },
  {
    path: '',
    children: [
      {
        path: 'home',
        component: HomeComponent,
      },
      {
        path: 'login',
        canActivate: [authenticateGuard],
        data: { flip: true },
        component: LoginComponent,
      },
      {
        path: 'register',
        canActivate: [authenticateGuard],
        data: { flip: true },
        component: RegisterComponent,
      },

      {
        path: 'staff',
        canActivate: [authorizeGuard],
        data: { roles: ['Administrator'] },
        component: StaffBuilderComponent,
      },
      {
        path: 'availabilitycommitment',
        canActivate: [authorizeGuard],
        data: { roles: ['User'] },
        component: AvailabilityAndCommitmentsComponent,
      },
      {
        path: 'schedule',
        canActivate: [authorizeGuard],
        data: { roles: ['Scheduler'] },
        component: ScheduleComponent,
      },
      {
        path: 'history',
        canActivate: [authenticateGuard],
        data: { flip: false },
        component: ScheduleHistoryComponent,
      },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
