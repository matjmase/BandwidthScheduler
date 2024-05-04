import { Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { BackendConnectService } from '../../services/backend-connect.service';
import { StandardSnackbarService } from '../../services/standard-snackbar.service';
import { HttpErrorResponse } from '@angular/common/http';
import { SpinnerCardHorizontalStretch } from '../../commonControls/spinner-card/spinner-card.component';

@Component({
  selector: 'app-add-team',
  templateUrl: './add-team.component.html',
  styleUrl: './add-team.component.scss',
})
export class AddTeamComponent {
  @Output() TeamAdded: EventEmitter<void> = new EventEmitter<void>();
  waitingOnSubmit: boolean = false;
  public SpinnerCardStretch: SpinnerCardHorizontalStretch =
    SpinnerCardHorizontalStretch.Grow;

  constructor(
    private messageSnackBar: StandardSnackbarService,
    private backend: BackendConnectService
  ) {}

  public AddTeamSubmit(form: NgForm): void {
    this.waitingOnSubmit = true;
    const teamName = form.value.teamName;

    this.backend.Staff.PostTeam(teamName).subscribe({
      complete: () => {
        this.messageSnackBar.OpenConfirmationMessage(
          'Team - ' + teamName + ' added successfully'
        );
        this.TeamAdded.emit();
        form.resetForm();
        this.waitingOnSubmit = false;
      },
      error: (errorResp: HttpErrorResponse) => {
        this.messageSnackBar.OpenErrorMessage(errorResp.error);
        this.waitingOnSubmit = false;
      },
    });
  }
}
