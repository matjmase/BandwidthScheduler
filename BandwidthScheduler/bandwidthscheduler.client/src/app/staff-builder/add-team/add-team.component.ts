import { Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BackendConnectService } from '../../services/backend-connect.service';
import { ITeam } from '../../models/ITeam';

@Component({
  selector: 'app-add-team',
  templateUrl: './add-team.component.html',
  styleUrl: './add-team.component.scss',
})
export class AddTeamComponent {
  @Output() TeamAdded: EventEmitter<void> = new EventEmitter<void>();

  constructor(
    private messageSnackBar: MatSnackBar,
    private backend: BackendConnectService
  ) {}

  private openSnackBar(message: string): void {
    this.messageSnackBar.open(message, 'Dismiss', {
      duration: 1000 * 3,
    });
  }

  public AddTeamSubmit(form: NgForm): void {
    const teamName = form.value.teamName;

    this.backend.Staff.PostTeam(teamName).subscribe({
      complete: () => {
        this.openSnackBar('Team - ' + teamName + ' added successfully');
        this.TeamAdded.emit();
        form.resetForm();
      },
      error: () => this.openSnackBar('Error adding team'),
    });
  }
}
