import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { StaffBuilderComponent } from './staff-builder/staff-builder.component';
import { AvailabilityBuilderComponent } from './availability-builder/availability-builder.component';
import { SchedulePublisherComponent } from './schedule-publisher/schedule-publisher.component';
import { ScheduleHistoryComponent } from './schedule-history/schedule-history.component';
import { authorizeGuard } from './guards/authorize.guard';
import { authenticateGuard } from './guards/authenticate.guard';
import { ScheduleEditorComponent } from './schedule-editor/schedule-editor.component';

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
        path: 'availability',
        canActivate: [authorizeGuard],
        data: { roles: ['User'] },
        component: AvailabilityBuilderComponent,
      },
      {
        path: 'publisher',
        canActivate: [authorizeGuard],
        data: { roles: ['Scheduler'] },
        component: SchedulePublisherComponent,
      },
      {
        path: 'editor',
        canActivate: [authorizeGuard],
        data: { roles: ['Scheduler'] },
        component: ScheduleEditorComponent,
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
