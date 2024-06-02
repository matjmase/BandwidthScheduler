import {
  Component,
  EventEmitter,
  Input,
  Output,
  ViewChild,
} from '@angular/core';
import { ITeam } from '../../models/db/ITeam';
import { SpinnerCardHorizontalStretch } from '../../commonControls/spinner-card/spinner-card.component';
import { TeamSelectorContainerComponent } from '../TeamSelectorContainerComponent';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, NgForm } from '@angular/forms';
import { BackendConnectService } from '../../services/backend-connect.service';
import { StandardSnackbarService } from '../../services/standard-snackbar.service';
import { TeamSelectorComponent } from '../../commonControls/team-selector/team-selector.component';

@Component({
  selector: 'app-edit-team',
  templateUrl: './edit-team.component.html',
  styleUrl: './edit-team.component.scss',
})
export class EditTeamComponent extends TeamSelectorContainerComponent {
  public NameEditControl = new FormControl('');

  public TeamName: string = '';

  constructor(
    private messageSnackBar: StandardSnackbarService,
    private backend: BackendConnectService
  ) {
    super();
  }

  protected override OnTeamSelected(team: ITeam): void {
    this.TeamName = team.name;
  }
  public override GetHorizontalStretch(): SpinnerCardHorizontalStretch {
    return SpinnerCardHorizontalStretch.Grow;
  }

  public EditTeamSubmit(): void {
    this.WaitingOnSubmit = true;
    const updatedTeam: ITeam = {
      id: this.DbTeam!.id,
      name: this.TeamName,
      commitments: undefined,
    };

    this.backend.Team.UpdateTeam(updatedTeam).subscribe({
      complete: () => {
        this.messageSnackBar.OpenConfirmationMessage(
          'Team - ' + updatedTeam.name + ' edited successfully'
        );
        this.TeamsUpdated.emit();
        this.WaitingOnSubmit = false;
        this.TeamCollectionUpdated();
      },
      error: (errorResp: HttpErrorResponse) => {
        this.messageSnackBar.OpenErrorMessage(errorResp.error);
        this.WaitingOnSubmit = false;
      },
    });
  }

  protected override ResetTeamSelector(): void {
    this.TeamName = '';

    this.NameEditControl.reset();
  }
}
