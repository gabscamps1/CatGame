using UnityEngine;

public class ParticleService : MonoBehaviour, IParticleService
{/*
    #region Pooling

    /// <summary>
    /// Cria um audio source configurado.
    /// </summary>
    /// <returns></returns>
    private AudioSource CreateParticleSource()
    {
        // Instancia uma nova particula.
        ParticleSystem particle = Instantiate(audioSourcePrefab, transform.position, Quaternion.identity, transform.parent);

        // Configura a particula.


        return particle;
    }

    /// <summary>
    /// Pega um audio source da fila.
    /// </summary>
    /// <returns></returns>
    private AudioSource GetParticleSource()
    {
        if (audioSourceQueue.Count > 0)
        {
            AudioSource audioSource = audioSourceQueue.Dequeue();
            audioSource.gameObject.SetActive(true);
            return audioSource;
        }
        else
        {
            return CreateParticleSource();
        }
    }

    private IEnumerator EnqueueParticleSource(AudioSource audioSource, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        audioSource.gameObject.SetActive(false);
        audioSource.clip = null;
        audioSourceQueue.Enqueue(audioSource);
    }

    #endregion
*/
    public void PlayParticle(ParticleSystem particle)
    {
        throw new System.NotImplementedException();
    }
}
