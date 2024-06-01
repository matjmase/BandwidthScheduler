import {
  HttpClientModule,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { NavBarComponent } from './nav-bar/nav-bar.component';
import { HomeComponent } from './home/home.component';
import { MatToolbarModule } from '@angular/material/toolbar';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { LoginComponent } from './login/login.component';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { RegisterComponent } from './register/register.component';
import { RegisterFormValidatorDirective } from './validators/register-form-validator.directive';
import { LoginFormValidatorDirective } from './validators/login-form-validator.directive';
import { MatSelectModule } from '@angular/material/select';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { authInterceptor } from './interceptors/auth.interceptor';
import { AuthenticateDirective } from './directives/authenticate.directive';
import { StaffBuilderComponent } from './staff-builder/staff-builder.component';
import { ScheduleHistoryComponent } from './schedule-history/schedule-history.component';
import { AuthorizeDirective } from './directives/authorize.directive';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { TeamSelectorComponent } from './commonControls/team-selector/team-selector.component';
import { AddRemoveTeamUserComponent } from './staff-builder/add-remove-team-user/add-remove-team-user.component';
import { AddTeamComponent } from './staff-builder/add-team/add-team.component';
import { ColorElementDirective } from './directives/color-element.directive';
import { SpinnerCardComponent } from './commonControls/spinner-card/spinner-card.component';
import { TimePickerSnapComponent } from './commonControls/time-picker-snap/time-picker-snap.component';
import { DateTimeRangeSelectorComponent } from './commonControls/date-time-range-selector/date-time-range-selector.component';
import { DateTimeRangeSelectorValidatorDirective } from './validators/date-time-range-selector-validator.directive';
import { UserLegendComponent } from './commonControls/user-legend/user-legend.component';
import { ScheduleComponent } from './schedule/schedule.component';
import { GridLegendReadOnlyComponent } from './schedule/common/grid-legend-read-only/grid-legend-read-only.component';
import { GridRenderingFormComponent } from './schedule/schedule-publisher/grid-rendering-form/grid-rendering-form.component';
import { GridRenderingGeneratedComponent } from './schedule/schedule-publisher/grid-rendering-generated/grid-rendering-generated.component';
import { GridRenderingProposalComponent } from './schedule/schedule-publisher/grid-rendering-proposal/grid-rendering-proposal.component';
import { SchedulePublisherComponent } from './schedule/schedule-publisher/schedule-publisher.component';
import { ScheduleRecallComponent } from './schedule/schedule-recall/schedule-recall.component';
import { MessageModalBoxComponent } from './commonControls/message-modal-box/message-modal-box.component';
import { AvailabilityAndCommitmentsComponent } from './availability-and-commitments/availability-and-commitments.component';
import { AvailabilityComponent } from './availability-and-commitments/availability/availability.component';
import { CommitmentComponent } from './availability-and-commitments/commitment/commitment.component';
import { RemoveTeamComponent } from './staff-builder/remove-team/remove-team.component';
import { EditTeamComponent } from './staff-builder/edit-team/edit-team.component';

@NgModule({
  declarations: [
    AppComponent,
    NavBarComponent,
    HomeComponent,
    LoginComponent,
    RegisterComponent,
    RegisterFormValidatorDirective,
    LoginFormValidatorDirective,
    AuthenticateDirective,
    StaffBuilderComponent,
    SchedulePublisherComponent,
    ScheduleHistoryComponent,
    AuthorizeDirective,
    TeamSelectorComponent,
    AddRemoveTeamUserComponent,
    AddTeamComponent,
    GridRenderingFormComponent,
    GridRenderingProposalComponent,
    GridRenderingGeneratedComponent,
    ColorElementDirective,
    SpinnerCardComponent,
    TimePickerSnapComponent,
    DateTimeRangeSelectorComponent,
    DateTimeRangeSelectorValidatorDirective,
    UserLegendComponent,
    ScheduleRecallComponent,
    GridLegendReadOnlyComponent,
    ScheduleComponent,
    MessageModalBoxComponent,
    AvailabilityAndCommitmentsComponent,
    AvailabilityComponent,
    CommitmentComponent,
    RemoveTeamComponent,
    EditTeamComponent,
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    BrowserAnimationsModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatCardModule,
    MatInputModule,
    MatSelectModule,
    NgbModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatDatepickerModule,
    MatButtonToggleModule,
    MatDividerModule,
    MatListModule,
    MatAutocompleteModule,
    MatSnackBarModule,
    MatTabsModule,
  ],
  providers: [
    provideAnimationsAsync(),
    provideNativeDateAdapter(),
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
