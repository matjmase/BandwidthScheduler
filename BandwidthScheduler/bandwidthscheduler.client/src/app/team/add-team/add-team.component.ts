import { Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { BackendConnectService } from '../../services/backend-connect.service';
import { StandardSnackbarService } from '../../services/standard-snackbar.service';
import { HttpErrorResponse } from '@angular/common/http';
import { SpinnerCardContentsComponent } from '../SpinnerCardContentsComponent';
import { SpinnerCardHorizontalStretch } from '../../commonControls/spinner-card/spinner-card.component';

@Component({
  selector: 'app-add-team',
  templateUrl: './add-team.component.html',
  styleUrl: './add-team.component.scss',
})
export class AddTeamComponent extends SpinnerCardContentsComponent {
  @Output() TeamAdded: EventEmitter<void> = new EventEmitter<void>();

  constructor(
    private messageSnackBar: StandardSnackbarService,
    private backend: BackendConnectService
  ) {
    super();
  }

  public override GetHorizontalStretch(): SpinnerCardHorizontalStretch {
    return SpinnerCardHorizontalStretch.Grow;
  }

  public AddTeamSubmit(form: NgForm): void {
    console.log(form);
    this.WaitingOnSubmit = true;
    const teamName = form.value.teamName;

    this.backend.Team.PostTeam(teamName).subscribe({
      complete: () => {
        this.messageSnackBar.OpenConfirmationMessage(
          'Team - ' + teamName + ' added successfully'
        );
        this.TeamAdded.emit();
        form.resetForm();
        this.WaitingOnSubmit = false;
      },
      error: (errorResp: HttpErrorResponse) => {
        this.messageSnackBar.OpenErrorMessage(errorResp.error);
        this.WaitingOnSubmit = false;
      },
    });
  }
}
